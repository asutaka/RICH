using CoinPr.DAL.Entity;
using CoinPr.Model;
using CoinPr.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using System.Net.Http.Headers;
using System.Text;

namespace CoinPr.Service
{
    public interface IAPIService
    {
        Task<List<Quote>> GetData(string symbol, EExchange exchange, EInterval interval);
        Task<List<Coin>> GetBinanceSymbol();
        Task<List<BybitSymbolDetail>> GetBybitSymbol();
        Task<List<Quote>> GetCoinData_Binance(string coin, string mode, long fromTime);
        Task<List<Quote>> GetCoinData_Binance(string coin, int num, string mode);
        Task<CoinAnk_LiquidValue> CoinAnk_GetLiquidValue(string coin);
    }
    public class APIService : IAPIService
    {
        private readonly ILogger<APIService> _logger;
        private readonly IHttpClientFactory _client;
        public APIService(ILogger<APIService> logger,
                        IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _client = httpClientFactory;
        }

        public async Task<List<Quote>> GetData(string symbol, EExchange exchange, EInterval interval)
        {
            try
            {
                if (exchange == EExchange.Bybit)
                {
                    Bybit.Net.Enums.KlineInterval bybitInterval = Bybit.Net.Enums.KlineInterval.FifteenMinutes;
                    if(interval == EInterval.H1)
                    {
                        bybitInterval = Bybit.Net.Enums.KlineInterval.OneHour;
                    }
                    else if(interval == EInterval.H4)
                    {
                        bybitInterval = Bybit.Net.Enums.KlineInterval.FourHours;
                    }
                    else if(interval == EInterval.D1)
                    {
                        bybitInterval = Bybit.Net.Enums.KlineInterval.OneDay;
                    }
                    else if(interval == EInterval.W1)
                    {
                        bybitInterval = Bybit.Net.Enums.KlineInterval.OneWeek;
                    }

                    var lByBit = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetKlinesAsync(Bybit.Net.Enums.Category.Linear, symbol, bybitInterval);
                    var lDataBybit = lByBit.Data.List.Select(x => new Quote
                    {
                        Date = x.StartTime,
                        Open = x.OpenPrice,
                        High = x.HighPrice,
                        Low = x.LowPrice,
                        Close = x.ClosePrice,
                        Volume = x.Volume,
                    }).ToList();

                    return lDataBybit;
                }

                Binance.Net.Enums.KlineInterval BinanceInterval = Binance.Net.Enums.KlineInterval.FifteenMinutes;
                if (interval == EInterval.H1)
                {
                    BinanceInterval = Binance.Net.Enums.KlineInterval.OneHour;
                }
                else if (interval == EInterval.H4)
                {
                    BinanceInterval = Binance.Net.Enums.KlineInterval.FourHour;
                }
                else if (interval == EInterval.D1)
                {
                    BinanceInterval = Binance.Net.Enums.KlineInterval.OneDay;
                }
                else if (interval == EInterval.W1)
                {
                    BinanceInterval = Binance.Net.Enums.KlineInterval.OneWeek;
                }
                var lBinance = await StaticVal.BinanceInstance().UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, BinanceInterval, limit: 500);
                var lDataBinance = lBinance.Data.Select(x => new Quote
                {
                    Date = x.OpenTime,
                    Open = x.OpenPrice,
                    High = x.HighPrice,
                    Low = x.LowPrice,
                    Close = x.ClosePrice,
                    Volume = x.Volume,
                }).ToList();

                return lDataBinance;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetData|EXCEPTION|INPUT: {symbol}| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Coin>> GetBinanceSymbol()
        {
            var url = "https://api3.binance.com/sapi/v1/convert/exchangeInfo?toAsset=USDT";
            try
            {
                using var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Content = new StringContent("",
                                                    Encoding.UTF8,
                                                    "application/json");

                var response = await client.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();
                if (contents.Length < 200)
                    return new List<Coin>();
                var res = JsonConvert.DeserializeObject<List<Coin>>(contents);
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetBinanceSymbol|EXCEPTION| {ex.Message}");
            }
            return new List<Coin>();
        }

        public async Task<List<BybitSymbolDetail>> GetBybitSymbol()
        {
            var url = "https://api2.bybitglobal.com/spot/api/basic/symbol_list_v3";
            try
            {
                using var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var contents = await responseMessage.Content.ReadAsStringAsync();
                if (contents.Length < 200)
                    return new List<BybitSymbolDetail>();
                var res = JsonConvert.DeserializeObject<BybitSymbol>(contents);
                return res.result.quoteTokenResult.FirstOrDefault(x => x.tokenId == "USDT")?.quoteTokenSymbols ?? new List<BybitSymbolDetail>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetBinanceSymbol|EXCEPTION| {ex.Message}");
            }
            return new List<BybitSymbolDetail>();
        }

        public async Task<List<Quote>> GetCoinData_Binance(string coin, int num, string mode)
        {
            var dt = DateTime.Now;
            long div = 0;
            if (mode == "5m")
            {
                div = 3;
            }
            else if (mode == "15m")
            {
                div = 10;
            }
            else if (mode == "1h")
            {
                div = 40;
            }
            else if (mode == "4h")
            {
                div = 150;
            }
            else if (mode == "1d")
            {
                div = 900;
            }
            var lQuote = new List<Quote>();
            var count = 0;
            var start = new DateTimeOffset(dt.AddDays(-div)).ToUnixTimeMilliseconds();
            var end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            do
            {
                var url = string.Format("https://api3.binance.com/api/v3/klines?symbol={0}&interval={1}&startTime={2}&endTime={3}&limit=1000", coin, mode, start, end);
                try
                {
                    using var client = _client.CreateClient();
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders
                          .Accept
                          .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                    request.Content = new StringContent("",
                                                        Encoding.UTF8,
                                                        "application/json");

                    var response = await client.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();
                    if (contents.Length < 200)
                    {
                        lQuote.Reverse();
                        return lQuote;
                    }
                    var lArray = JArray.Parse(contents);
                    if (lArray.Any())
                    {
                        var lOut = lArray.Select(x => new Quote
                        {
                            Date = long.Parse(x[0].ToString()).UnixTimeStampMinisecondToDateTime(),
                            Open = decimal.Parse(x[1].ToString()),
                            High = decimal.Parse(x[2].ToString()),
                            Low = decimal.Parse(x[3].ToString()),
                            Close = decimal.Parse(x[4].ToString()),
                            Volume = decimal.Parse(x[5].ToString()),
                        }).ToList();
                        lOut.Reverse();
                        lQuote.AddRange(lOut);
                        count += lOut.Count();
                        if (count >= num)
                        {
                            lQuote.Reverse();
                            return lQuote;
                        }
                    }
                    else
                    {
                        lQuote.Reverse();
                        return lQuote;
                    }
                    end = start - 1;
                    start = start - div * 86400000;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"APIService.GetCoinData_Binance|EXCEPTION|INPUT: coin:{coin}| {ex.Message}");
                }
            }
            while (true);
        }

        public async Task<List<Quote>> GetCoinData_Binance(string coin, string mode, long fromTime)
        {
            var url = string.Format("https://api3.binance.com/api/v3/klines?symbol={0}&interval={1}&startTime={2}&limit=1000", coin, mode, fromTime);
            try
            {
                using var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Content = new StringContent("",
                                                    Encoding.UTF8,
                                                    "application/json");

                var response = await client.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();
                if (contents.Length < 200)
                    return new List<Quote>();

                var lArray = JArray.Parse(contents);
                if (lArray.Any())
                {
                    var lOut = lArray.Select(x => new Quote
                    {
                        Date = long.Parse(x[0].ToString()).UnixTimeStampMinisecondToDateTime(),
                        Open = decimal.Parse(x[1].ToString()),
                        High = decimal.Parse(x[2].ToString()),
                        Low = decimal.Parse(x[3].ToString()),
                        Close = decimal.Parse(x[4].ToString()),
                        Volume = decimal.Parse(x[5].ToString()),
                    }).ToList();
                    return lOut;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetCoinData_Binance|EXCEPTION|INPUT: coin:{coin}| {ex.Message}");
            }
            return new List<Quote>();
        }

        private string CoinAnk_GetKey()
        {
            var str = "-b31e-c547-d299-b6d07b7631aba2c903cc|";
            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            time += 1111111111111;
            var text = $"{str}{time}347".Base64Encode();
            return text;
        }

        public async Task<CoinAnk_LiquidValue> CoinAnk_GetLiquidValue(string coin)
        {
            var url = $"https://api.coinank.com/api/liqMap/getLiqHeatMap?exchangeName=Binance&symbol={coin}&interval=1d";
            try
            {
                using var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("coinank-apikey", CoinAnk_GetKey());
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var contents = await responseMessage.Content.ReadAsStringAsync();
                if (contents.Length < 200)
                    return new CoinAnk_LiquidValue();
                var res = JsonConvert.DeserializeObject<CoinAnk_LiquidValue>(contents);
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetLiquidValue|EXCEPTION| {ex.Message}");
            }
            return new CoinAnk_LiquidValue();
        }
    }
}
