using System.Text;
using System.Net.Http;

namespace StockPr.Service
{
    public interface IHighChartService
    {
        Task<Stream> GetChartImage(string body);
    }

    public class HighChartService : IHighChartService
    {
        private readonly ILogger<HighChartService> _logger;
        private readonly IHttpClientFactory _client;

        public HighChartService(ILogger<HighChartService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _client = httpClientFactory;
        }

        public async Task<Stream> GetChartImage(string body)
        {
            try
            {
                var url = "http://127.0.0.1:7801"; // Local Highcharts Export Server
                var client = _client.CreateClient("ResilientClient");
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "");
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.IsSuccessStatusCode)
                {
                    return await responseMessage.Content.ReadAsStreamAsync();
                }
                
                var errorMsg = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogError($"HighChartService.GetChartImage|ERROR| Status: {responseMessage.StatusCode}, Content: {errorMsg}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"HighChartService.GetChartImage|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
