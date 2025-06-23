using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using System.Net.Http.Headers;
using System.Text;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace CoinUtilsPr
{
    public interface IAPIService
    {
        Task<List<Quote>> GetData_Bybit(string symbol, int DAY = 10, int SKIP_DAY = 0);
        Task<List<Quote>> GetData_Binance(string symbol, EInterval interval, long fromTime = 0);
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

        private async Task<List<Quote>> GetData_Bybit(string symbol, DateTimeOffset from, DateTimeOffset to)
        {
            var url = string.Format("https://www.bybitglobal.com/x-api/contract/v5/public/instrument/kline/market?contract_type=2&symbol={0}&resolution=15&from={1}&to={2}", symbol, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds());

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
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");

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
                _logger.LogError(ex, $"APIService.GetData_Bybit|EXCEPTION|INPUT: symbol:{symbol}| {ex.Message}");
            }
            return new List<Quote>();
        }
        public async Task<List<Quote>> GetData_Bybit(string symbol, int DAY = 10, int SKIP_DAY = 0)
        {
            var lAll = new List<Quote>();
            try
            {
                var now = DateTimeOffset.UtcNow;
                var from = now.AddDays(-DAY);
                var to = now.AddDays(-SKIP_DAY);
                var divDay = (int)(to - from).TotalDays;
                var even = Math.Ceiling((double)divDay / 10);
                for (int i = 0; i < even; i++)
                {
                    var fromTime = from.AddDays(i * 10);
                    var toTime = fromTime.AddDays(10);
                    var lres = await GetData_Bybit(symbol, fromTime, toTime);
                    Thread.Sleep(200);
                    if (lres.Any())
                    {
                        lAll.AddRange(lres);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetData_Bybit|EXCEPTION|INPUT: symbol:{symbol}| {ex.Message}");
            }
            return lAll;
        }

        public async Task<List<Quote>> GetData_Binance(string symbol, EInterval interval, long fromTime = 0)
        {
            //
            var url = string.Format("https://www.binance.com/fapi/v1/continuousKlines?startTime={2}&limit=1000&pair={0}&contractType=PERPETUAL&interval={1}", symbol, interval.GetDisplayName(), fromTime);
            if (fromTime <= 0)
            {
                url = string.Format("https://www.binance.com/fapi/v1/continuousKlines?limit=1000&pair={0}&contractType=PERPETUAL&interval={1}", symbol, interval.GetDisplayName()); ;
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
                _logger.LogError(ex, $"APIService.GetData_Binance|EXCEPTION|INPUT: symbol:{symbol}| {ex.Message}");
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
