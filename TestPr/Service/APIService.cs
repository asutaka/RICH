using Bybit.Net.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using System.Net.Http.Headers;
using System.Text;
using TestPr.Utils;

namespace TestPr.Service
{
    public interface IAPIService
    {
        Task<List<Quote>> GetData(string symbol, EInterval interval, long fromTime = 0);
        Task<List<Quote>> GetData_Bybit(string symbol, EInterval interval, long fromTime = 0);
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

        public async Task<List<Quote>> GetData(string symbol, EInterval interval, long fromTime = 0)
        {
            try
            {
                string intervalStr = "15m";
                if (interval == EInterval.H1)
                {
                    intervalStr = "1h";
                }
                else if (interval == EInterval.H4)
                {
                    intervalStr = "4h";
                }
                else if (interval == EInterval.D1)
                {
                    intervalStr = "1d";
                }
                else if (interval == EInterval.W1)
                {
                    intervalStr = "1w";
                }
                else if (interval == EInterval.M1)
                {
                    intervalStr = "1m";
                }
                var lDataBinance = await GetCoinData_Binance(symbol, intervalStr, fromTime);

                return lDataBinance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetData|EXCEPTION|INPUT: {symbol}| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Quote>> GetData_Bybit(string symbol, EInterval interval, long fromTime = 0)
        {
            try
            {
                var lres = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetMarkPriceKlinesAsync(Category.Linear, symbol, KlineInterval.FifteenMinutes, startTime: fromTime.UnixTimeStampMinisecondToDateTime(), limit: 1000);
                if(lres.Data.List.Any())
                {
                    return lres.Data.List.Reverse().Select(x => new Quote
                    {
                        Open = x.OpenPrice,
                        High = x.HighPrice,
                        Low = x.LowPrice,
                        Close = x.ClosePrice,
                        Date = x.StartTime
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetData_Bybit|EXCEPTION|INPUT: {symbol}| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Quote>> GetCoinData_Binance(string coin, string mode, long fromTime)
        {
            var url = string.Format("https://api3.binance.com/api/v3/klines?symbol={0}&interval={1}&startTime={2}&limit=1000", coin, mode, fromTime);
            if (fromTime <= 0)
            {
                url = string.Format("https://api3.binance.com/api/v3/klines?symbol={0}&interval={1}&limit=1000", coin, mode);
            }

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

        public async Task<List<Quote>> GetCoinData_Bybit(string coin, string mode, long fromTime)
        {
            var url = string.Format("https://www.bybitglobal.com/x-api/spot/api/quote/v5/klines?symbol={0}&interval={1}&limit=1000&from={2}", coin, mode, fromTime);
            if (fromTime <= 0)
            {
                url = string.Format("https://www.bybitglobal.com/x-api/spot/api/quote/v5/klines?symbol={0}&interval={1}&limit=1000", coin, mode);
            }

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

                var res = JsonConvert.DeserializeObject<clsBybit>(contents);

                if (res.result.list.Any())
                {
                    var lOut = res.result.list.Select(x => new Quote
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
                _logger.LogError(ex, $"APIService.GetCoinData_Bybit|EXCEPTION|INPUT: coin:{coin}| {ex.Message}");
            }
            return new List<Quote>();
        }
    }

    public class clsBybit
    {
        public clsBybitResult result { get; set; }
    }

    public class clsBybitResult
    {
        public List<List<string>> list { get; set; }
    }

}
