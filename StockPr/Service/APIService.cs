using System.Text;

namespace StockPr.Service
{
    public interface IAPIService
    {
        Task<Stream> GetChartImage(string body);
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
        public async Task<Stream> GetChartImage(string body)
        {
            try
            {
                //var url = "http://127.0.0.1:7801";//Local
                var url = "https://export.highcharts.com";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(10);
                var requestMessage = new HttpRequestMessage();
                //requestMessage.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var responseMessage = await client.SendAsync(requestMessage);
                var result = await responseMessage.Content.ReadAsStreamAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.GetChartImage|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
