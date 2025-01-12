using HtmlAgilityPack;
using Newtonsoft.Json;
using StockExtendPr.Model;
using System.Text;

namespace StockExtendPr.Service
{
    public interface IAPIService
    {
        Task<MacroMicro_Key> MacroMicro_WCI(string key);
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
        private async Task<(string, string)> MacroMicro_GetAuthorize()
        {
            try
            {
                var cookie = string.Empty;
                var url = $"https://en.macromicro.me/";
                var web = new HtmlWeb()
                {
                    UseCookies = true
                };
                web.PostResponse += (request, response) =>
                {
                    cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                };
                var document = web.Load(url);
                var html = document.ParsedText;
                var index = html.IndexOf("data-stk=");
                if (index < 0)
                    return (null, null);

                var sub = html.Substring(index + 10);
                var indexCut = sub.IndexOf("\"");
                if (indexCut < 0)
                    return (null, null);

                var authorize = sub.Substring(0, indexCut);
                return (authorize, cookie);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.MacroMicro_GetAuthorize|EXCEPTION| {ex.Message}");
            }
            return (null, null);
        }
        public async Task<MacroMicro_Key> MacroMicro_WCI(string key)
        {
            //wci: 44756
            //bdti: 946
            try
            {
                var res = await MacroMicro_GetAuthorize();
                if (string.IsNullOrWhiteSpace(res.Item1)
                    || string.IsNullOrWhiteSpace(res.Item2))
                    return null;

                var client = _client.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://en.macromicro.me/charts/data/{key}");
                request.Headers.Add("authorization", $"Bearer {res.Item1}");
                request.Headers.Add("cookie", res.Item2);
                request.Headers.Add("referer", "https://en.macromicro.me/collections/22190/sun-ming-te-investment-dashboard/44756/drewry-world-container-index");
                request.Headers.Add("user-agent", "zzz");

                request.Content = new StringContent(string.Empty,
                                    Encoding.UTF8,
                                    "application/json");//CONTENT-TYPE header
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMessageStr = await response.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<MacroMicro_Main>(responseMessageStr);
                if (key.Equals("946"))
                {
                    return responseModel?.data.key2;
                }
                return responseModel?.data.key;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.MacroMicro_WCI|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
