using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockPr.Model;
using StockPr.Parser;
using StockPr.Utils;
using System.Net;
using System.Text;

namespace StockPr.Service
{
    public interface IMarketDataService
    {
        Task<Stream> TuDoanhHSX(DateTime dt);
        Task<List<Quote>> Vietstock_GetDataStock(string code);
        Task<List<Quote>> SSI_GetDataStock(string code);
        Task<List<QuoteT>> SSI_GetDataStockT(string code);
        Task<List<Quote>> SSI_GetDataStock_HOUR(string code);
        Task<SSI_DataFinanceDetailResponse> SSI_GetFinanceStock(string code);
        Task<decimal> SSI_GetFreefloatStock(string code);
        Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code);
        Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code, DateTime from, DateTime to);
        Task<SSI_DataStockInfoResponse> SSI_GetStockInfo_Extend(string code, DateTime from, DateTime to);
        Task<VNDirect_ForeignDetailResponse> VNDirect_GetForeign(string code);
        Task<List<Money24h_PTKTResponse>> Money24h_GetMaTheoChiBao(string chibao);
        Task<List<Money24h_ForeignResponse>> Money24h_GetForeign(EExchange mode, EMoney24hTimeType type);
        Task<List<Money24h_TuDoanhResponse>> Money24h_GetTuDoanh(EExchange mode, EMoney24hTimeType type);
        Task<Money24h_NhomNganhResponse> Money24h_GetNhomNganh(EMoney24hTimeType type);
        Task<Money24h_StatisticResponse> Money24h_GetThongke(string sym = "10");
        Task SBV_OMO();
    }
    public class MarketDataService : IMarketDataService
    {
        private readonly ILogger<MarketDataService> _logger;
        private readonly IHttpClientFactory _client;
        private readonly IMarketDataParser _parser;
        public MarketDataService(ILogger<MarketDataService> logger, IHttpClientFactory httpClientFactory, IMarketDataParser parser)
        {
            _logger = logger;
            _client = httpClientFactory;
            _parser = parser;
        }

        public async Task<Stream> TuDoanhHSX(DateTime dt)
        {
            try
            {
                var url = "https://www.hsx.vn";
                var client = _client.CreateClient("ResilientClient");
                var responseMessage = await client.GetAsync(url);
                var html = await responseMessage.Content.ReadAsStringAsync();
                var link = _parser.ParseTuDoanhHSXLink(html, dt);

                if (string.IsNullOrWhiteSpace(link)) return null;

                var detailUrl = $"{url}{link.Replace("ViewArticle", "GetRelatedFiles")}?rows=30&page=1";
                var detailResult = await client.GetStringAsync(detailUrl);
                var model = JsonConvert.DeserializeObject<HSXTudoanhModel>(detailResult);
                var lastID = model.rows?.FirstOrDefault()?.cell?.FirstOrDefault();
                
                if (string.IsNullOrEmpty(lastID)) return null;

                var downloadUrl = $"{url}/Modules/CMS/Web/DownloadFile?id={lastID}";
                return await client.GetStreamAsync(downloadUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.TuDoanhHSX|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Quote>> Vietstock_GetDataStock(string code)
        {
            var lOutput = new List<Quote>();
            var now = DateTimeOffset.Now;
            var from = now.AddYears(-1).ToUnixTimeSeconds();
            var to = now.AddDays(1).ToUnixTimeSeconds();
            var url = $"https://api.vietstock.vn/tvnew/history?symbol={code}&resolution=1D&from={from}&to={to}";
            
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Origin", "https://stockchart.vietstock.vn");
                request.Headers.Add("Referer", "https://stockchart.vietstock.vn/");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36");

                var responseMessage = await client.SendAsync(request);
                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var resultArray = await responseMessage.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<SSI_DataTradingDetailResponse>(resultArray);
                    
                    if (responseModel?.t != null && responseModel.t.Any())
                    {
                        for (int i = 0; i < responseModel.t.Count(); i++)
                        {
                            lOutput.Add(new Quote
                            {
                                Date = responseModel.t.ElementAt(i).UnixTimeStampToDateTime(),
                                Open = responseModel.o.ElementAt(i),
                                High = responseModel.h.ElementAt(i),
                                Low = responseModel.l.ElementAt(i),
                                Close = responseModel.c.ElementAt(i),
                                Volume = responseModel.v.ElementAt(i)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.Vietstock_GetDataStock|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        public async Task<List<Quote>> SSI_GetDataStock(string code)
        {
            var lOutput = new List<Quote>();
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/charts/history?symbol={0}&resolution={1}&from={2}&to={3}";
            try
            {
                var url = string.Format(urlBase, code, "1D", DateTimeOffset.Now.AddYears(-2).ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<SSI_DataTradingResponse>(resultArray);
                if (responseModel?.data?.t != null)
                {
                    for (int i = 0; i < responseModel.data.t.Count(); i++)
                    {
                        lOutput.Add(new Quote
                        {
                            Date = responseModel.data.t.ElementAt(i).UnixTimeStampToDateTime(),
                            Open = responseModel.data.o.ElementAt(i),
                            Close = responseModel.data.c.ElementAt(i),
                            High = responseModel.data.h.ElementAt(i),
                            Low = responseModel.data.l.ElementAt(i),
                            Volume = responseModel.data.v.ElementAt(i)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetDataStock|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        public async Task<List<QuoteT>> SSI_GetDataStockT(string code)
        {
            var lOutput = new List<QuoteT>();
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/charts/history?symbol={0}&resolution={1}&from={2}&to={3}";
            try
            {
                var url = string.Format(urlBase, code, "1D", DateTimeOffset.Now.AddYears(-2).ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<SSI_DataTradingResponse>(resultArray);
                if (responseModel?.data?.t != null)
                {
                    for (int i = 0; i < responseModel.data.t.Count(); i++)
                    {
                        lOutput.Add(new QuoteT
                        {
                            TimeStamp = responseModel.data.t.ElementAt(i),
                            Date = responseModel.data.t.ElementAt(i).UnixTimeStampToDateTime(),
                            Open = responseModel.data.o.ElementAt(i),
                            Close = responseModel.data.c.ElementAt(i),
                            High = responseModel.data.h.ElementAt(i),
                            Low = responseModel.data.l.ElementAt(i),
                            Volume = responseModel.data.v.ElementAt(i)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetDataStockT|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        public async Task<List<Quote>> SSI_GetDataStock_HOUR(string code)
        {
            var lOutput = new List<Quote>();
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/charts/history?symbol={0}&resolution={1}&from={2}&to={3}";
            try
            {
                var url = string.Format(urlBase, code, "60", DateTimeOffset.Now.AddMonths(-1).ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<SSI_DataTradingResponse>(resultArray);
                if (responseModel?.data?.t != null)
                {
                    for (int i = 0; i < responseModel.data.t.Count(); i++)
                    {
                        lOutput.Add(new Quote
                        {
                            Date = responseModel.data.t.ElementAt(i).UnixTimeStampToDateTime(),
                            Open = responseModel.data.o.ElementAt(i),
                            Close = responseModel.data.c.ElementAt(i),
                            High = responseModel.data.h.ElementAt(i),
                            Low = responseModel.data.l.ElementAt(i),
                            Volume = responseModel.data.v.ElementAt(i)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetDataStock_HOUR|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        public async Task<SSI_DataFinanceDetailResponse> SSI_GetFinanceStock(string code)
        {
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/company/ssmi/finance-indicator?symbol={0}&page=1&pageSize=10";
            try
            {
                var url = string.Format(urlBase, code);
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<SSI_DataFinanceResponse>(resultArray);
                if (responseModel?.data != null && responseModel.data.Any())
                {
                    return responseModel.data.First();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetFinanceStock|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<decimal> SSI_GetFreefloatStock(string code)
        {
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/company/ssmi/share-holder-detail?symbol={0}&language=vn&page=1&pageSize=100";
            try
            {
                var url = string.Format(urlBase, code);
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<SSI_ShareholderResponse>(resultArray);
                if (responseModel?.data != null && responseModel.data.Any())
                {
                    return Math.Round(100 * (1 - responseModel.data.Sum(x => x.percentage)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetFreefloatStock|EXCEPTION| {ex.Message}");
            }
            return 0;
        }

        public async Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code)
        {
            var to = DateTime.Now;
            var from = to.AddMonths(-1);
            var url = $"https://iboard-api.ssi.com.vn/statistics/company/ssmi/stock-info?symbol={code}&page=1&pageSize=100&fromDate={from.Day.To2Digit()}%2F{from.Month.To2Digit()}%2F{from.Year}&toDate={to.Day.To2Digit()}%2F{to.Month.To2Digit()}%2F{to.Year}";
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<SSI_DataStockInfoResponse>(resultArray);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetStockInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code, DateTime from, DateTime to)
        {
            var url = $"https://iboard-api.ssi.com.vn/statistics/company/ssmi/stock-info?symbol={code}&page=1&pageSize=100&fromDate={from.Day.To2Digit()}%2F{from.Month.To2Digit()}%2F{from.Year}&toDate={to.Day.To2Digit()}%2F{to.Month.To2Digit()}%2F{to.Year}";
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<SSI_DataStockInfoResponse>(resultArray);
                if (responseModel?.data != null)
                {
                    foreach (var item in responseModel.data)
                    {
                        item.netTotalTradeVol = item.totalBuyTradeVol - item.totalSellTradeVol;
                    }
                }
                return responseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetStockInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<SSI_DataStockInfoResponse> SSI_GetStockInfo_Extend(string code, DateTime from, DateTime to)
        {
            try
            {
                var info = await SSI_GetStockInfo(code, from, to);
                if (info == null) return null;

                var info_VNDirect = await VNDirect_GetForeign(code);
                if (info_VNDirect == null) return info;

                var firstDate = info.data.First().tradingDate.ToDateTime("dd/MM/yyyy");
                var vnDirectDate = info_VNDirect.tradingDate.ToDateTime("yyyy-MM-dd");
                var div = (vnDirectDate - firstDate).TotalDays;
                
                if (div == 0)
                {
                    info.data.First().netBuySellVal = info_VNDirect.netVal;
                    info.data.First().netBuySellVol = info_VNDirect.netVol;
                }
                else if (div < 10)
                {
                    var add = new SSI_DataStockInfoDetailResponse
                    {
                        tradingDate = vnDirectDate.ToString("dd/MM/yyyy"),
                        netBuySellVal = info_VNDirect.netVal,
                        netBuySellVol = info_VNDirect.netVol
                    };
                    info.data.Insert(0, add);
                }
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.SSI_GetStockInfo_Extend|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<VNDirect_ForeignDetailResponse> VNDirect_GetForeign(string code)
        {
            var url = $"https://api-finfo.vndirect.com.vn/v4/foreigns/latest?order=tradingDate&filter=code:{code}";
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var responseStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VNDirect_ForeignResponse>(responseStr);
                return responseModel?.data?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.VNDirect_GetForeign|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Money24h_PTKTResponse>> Money24h_GetMaTheoChiBao(string chibao)
        {
            try
            {
                var body = "{\"floor_codes\":[\"10\",\"11\",\"02\",\"03\"],\"group_ids\":[\"0001\",\"1000\",\"2000\",\"3000\",\"4000\",\"5000\",\"6000\",\"7000\",\"8000\",\"8301\",\"9000\"],\"signals\":[{\"" + chibao + "\":\"up\"}]}";
                var url = "https://api-finance-t19.24hmoney.vn/v2/web/indices/technical-signal-filter?sort=asc&page=1&per_page=50";
                var client = _client.CreateClient("ResilientClient");
                var responseMessage = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<Money24h_PTKT_LV1Response>(resultArray);
                
                if (responseModel?.data?.data != null && responseModel.status == 200)
                {
                    return responseModel.data.data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.Money24h_GetMaTheoChiBao|EXCEPTION| {ex.Message}");
            }
            return new List<Money24h_PTKTResponse>();
        }

        public async Task<List<Money24h_ForeignResponse>> Money24h_GetForeign(EExchange mode, EMoney24hTimeType type)
        {
            try
            {
                var url = $"https://api-finance-t19.24hmoney.vn/v2/web/indices/foreign-trading-all-stock-by-time?code={mode.GetDisplayName()}&type={type.GetDisplayName()}";
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<Money24h_ForeignAPIResponse>(resultArray);
                
                if (responseModel?.data?.data != null && responseModel.status == 200)
                {
                    var date = responseModel.data.from_date.ToDateTime("dd/MM/yyyy");
                    return responseModel.data.data.Where(x => x.symbol.Length == 3)
                        .OrderByDescending(x => x.net_val)
                        .Select((x, index) => new Money24h_ForeignResponse
                        {
                            no = index + 1,
                            d = new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeSeconds(),
                            s = x.symbol,
                            sell_qtty = x.sell_qtty,
                            sell_val = x.sell_val,
                            buy_qtty = x.buy_qtty,
                            buy_val = x.buy_val,
                            net_val = x.net_val,
                            t = DateTimeOffset.Now.ToUnixTimeSeconds()
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.Money24h_GetForeign|EXCEPTION| {ex.Message}");
            }
            return new List<Money24h_ForeignResponse>();
        }

        public async Task<List<Money24h_TuDoanhResponse>> Money24h_GetTuDoanh(EExchange mode, EMoney24hTimeType type)
        {
            try
            {
                var url = $"https://api-finance-t19.24hmoney.vn/v2/web/indices/proprietary-trading-all-stock-by-time?code={mode.GetDisplayName()}&type={type.GetDisplayName()}";
                var client = _client.CreateClient("ResilientClient");
                var resultArray = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<Money24h_TuDoanhAPIResponse>(resultArray);
                
                if (responseModel?.data?.data != null && responseModel.status == 200)
                {
                    var date = responseModel.data.from_date.ToDateTime("dd/MM/yyyy");
                    return responseModel.data.data.Where(x => x.symbol.Length == 3)
                        .OrderByDescending(x => x.prop_net)
                        .Select((x, index) => new Money24h_TuDoanhResponse
                        {
                            no = index + 1,
                            d = new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeSeconds(),
                            s = x.symbol,
                            prop_net_deal = x.prop_net_deal,
                            prop_net_pt = x.prop_net_pt,
                            prop_net = x.prop_net,
                            t = DateTimeOffset.Now.ToUnixTimeSeconds()
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.Money24h_GetTuDoanh|EXCEPTION| {ex.Message}");
            }
            return new List<Money24h_TuDoanhResponse>();
        }

        public async Task<Money24h_NhomNganhResponse> Money24h_GetNhomNganh(EMoney24hTimeType type)
        {
            try
            {
                var url = $"https://api-finance-t19.24hmoney.vn/v2/ios/company-group/all-level-with-summary?type={type.GetDisplayName()}";
                var client = _client.CreateClient("ResilientClient");
                var result = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<Money24h_NhomNganhResponse>(result);
                if (responseModel?.status == 200) return responseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.Money24h_GetNhomNganh|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<Money24h_StatisticResponse> Money24h_GetThongke(string sym = "10")
        {
            try
            {
                var url = $"https://api-finance-t19.24hmoney.vn/v1/ios/stock/statistic-investor-history?symbol={sym}";
                var client = _client.CreateClient("ResilientClient");
                var result = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<Money24h_StatisticResponse>(result);
                if (responseModel?.status == 200) return responseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MarketDataService.Money24h_GetThongke|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task SBV_OMO()
        {
            // Logic currently commented out in original file
            await Task.CompletedTask;
        }
    }
}
