using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using System.Net.Http.Headers;
using System.Text;
using CoinUtilsPr.Model;

namespace CoinUtilsPr
{
    public interface IAPIService
    {
        Task<List<Quote>> GetData_Bybit(string symbol, int DAY = 10, int SKIP_DAY = 0);
        Task<List<Quote>> GetData_Binance(string symbol, EInterval interval);
        Task<List<Quote>> GetData_Binance(string symbol, EInterval interval, int DAY = 10, int SKIP_DAY = 0);
        Task<List<Quote>> GetData_Bybit_1H(string symbol);
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

        private async Task<List<Quote>> GetData_Bybit(string symbol, DateTimeOffset from, DateTimeOffset to)
        {
            var url = string.Format("https://www.bybitglobal.com/x-api/contract/v5/public/instrument/kline/market?contract_type=2&symbol={0}&resolution=15&from={1}&to={2}", symbol, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds());

            try
            {
                using var client = _client.CreateClient("ConfiguredHttpMessageHandler");
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Content = new StringContent("",
                                                    Encoding.UTF8,
                                                    "application/json");
                request.Headers.Add("User-Agent", "PostmanRuntime/7.43.4");

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

        public async Task<List<Quote>> GetData_Bybit_1H(string symbol)
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddMonths(-1).ToUnixTimeSeconds();
            var to = now.ToUnixTimeSeconds();

            var url = string.Format("https://www.bybitglobal.com/x-api/contract/v5/public/instrument/kline/market?contract_type=2&symbol={0}&resolution=60&from={1}&to={2}", symbol, from, to);

            try
            {
                using var client = _client.CreateClient("ConfiguredHttpMessageHandler");
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "");
                request.Content = new StringContent("",
                                                    Encoding.UTF8,
                                                    "application/json");
                request.Headers.Add("User-Agent", "PostmanRuntime/7.43.4");

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

        public async Task<List<Quote>> GetData_Binance(string symbol, EInterval interval, int DAY = 10, int SKIP_DAY = 0)
        {
            var lAll = new List<Quote>();
            try
            {
                var now = DateTimeOffset.UtcNow;
                var from = now.AddDays(-DAY);
                var to = now.AddDays(-SKIP_DAY);
                var divDay = (int)(to - from).TotalDays;
                var songay = 10; //Ứng với khung 15m
                if (interval == EInterval.H1)
                {
                    songay = 40;
                }
                var even = Math.Ceiling((double)divDay / songay);
                for (int i = 0; i < even; i++)
                {
                    var fromTime = from.AddDays(i * songay);
                    var toTime = fromTime.AddDays(songay);
                    var lres = await GetData_Binance(symbol, interval, fromTime, toTime);
                    Thread.Sleep(200);
                    if (lres.Any())
                    {
                        lAll.AddRange(lres);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"APIService.GetData_Binance|EXCEPTION|INPUT: symbol:{symbol}| {ex.Message}");
            }
            return lAll;
        }

        public async Task<List<Quote>> GetData_Binance(string symbol, EInterval interval, DateTimeOffset from, DateTimeOffset to)
        {
            var url = string.Format("https://www.binance.com/fapi/v1/continuousKlines?startTime={2}&endTime={3}&pair={0}&contractType=PERPETUAL&interval={1}", symbol, interval.GetDisplayName(), from.ToUnixTimeMilliseconds(), to.ToUnixTimeMilliseconds());

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

        public async Task<List<Quote>> GetData_Binance(string symbol, EInterval interval)
        {
            //
            //var url = string.Format("https://www.binance.com/fapi/v1/continuousKlines?startTime={2}&limit=1000&pair={0}&contractType=PERPETUAL&interval={1}", symbol, interval.GetDisplayName(), fromTime);
            //if (fromTime <= 0)
            //{

            //}

            var url = string.Format("https://www.binance.com/fapi/v1/continuousKlines?limit=1000&pair={0}&contractType=PERPETUAL&interval={1}", symbol, interval.GetDisplayName());

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
                Thread.Sleep(100);

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

    public class clsBybit
    {
        public clsBybitResult result { get; set; }
    }

    public class clsBybitResult
    {
        public List<List<string>> list { get; set; }
    }
}
