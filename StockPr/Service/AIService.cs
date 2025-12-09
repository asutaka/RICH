using Amazon.Runtime;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockPr.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPr.Service
{
    public interface IAIService
    {
        Task<(bool, string)> AskModel(string code);
    }
    public class AIService : IAIService
    {
        private readonly ILogger<AIService> _logger;
        private readonly IAPIService _apiService;
        private readonly IHttpClientFactory _client;
        public AIService(ILogger<AIService> logger, IAPIService api, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _apiService = api;
            _client = httpClientFactory;
        }
        #region AI
        public async Task<(bool, string)> AskModel(string code)
        {
            try
            {
                var dat = await GetData(code);
                if (dat is null)
                    return (false, null);

                var url = $"http://localhost:11434/api/chat";
                var sBuilder = new StringBuilder();
                sBuilder.AppendLine($"Dưới đây là dữ liệu của mã {code}. Hãy phân tích theo yêu cầu.");
                sBuilder.AppendLine(dat);
                sBuilder.AppendLine("Hãy phân tích theo format:\r\n1. Xu hướng\r\n2. Kỹ thuật\r\n3. Dòng tiền\r\n4. Rủi ro\r\n5. Kết luận");

                var body = new
                {
                    model = "llama3",
                    messages = new[]
                   {
                        new {
                            role = "system",
                            content =
                                "Bạn là chuyên gia phân tích chứng khoán. " +
                                "Nhiệm vụ: (1) đọc dữ liệu lịch sử giá và các chỉ báo, " +
                                "(2) phân tích dòng tiền tổ chức/cá nhân, " +
                                "(3) đánh giá xu hướng + rủi ro, " +
                                "(4) trả lời theo format: Xu hướng / Kỹ thuật / Dòng tiền / Rủi ro / Kết luận."
                        },
                        new {
                            role = "user",
                            content = sBuilder.ToString()    // prompt phải chứa JSON hoặc bảng dữ liệu
                        }
                    }
                };

                var client = _client.CreateClient();
                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json);
                var response = await client.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                dynamic data = JsonConvert.DeserializeObject(result);
                return (true, data.message.content.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AIService.AskModel|EXCEPTION| {ex.Message}");
            }

            return (false, null);
        }

        private async Task<string> GetData(string code)
        {
            try
            {
                var dat = await _apiService.SSI_GetDataStock(code);
                if (!dat.Any())
                    return null;

                var output = new clsModel
                {
                    s = code,
                    his = dat
                };
                return JsonConvert.SerializeObject(output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AIService.GetData|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private class clsModel
        {
            public string s { get; set; }
            public List<Quote> his { get; set; }
        }
        #endregion
    }
}
