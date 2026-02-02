using Newtonsoft.Json;
using StockExtendPr.Model;
using System.Net;
using System.Text;
using HtmlAgilityPack;

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
                var url = "https://en.macromicro.me/";
                var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";

                // Use curl.exe as a workaround for TLS fingerprinting blocks on HttpClient
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "curl.exe",
                    Arguments = $"-i -s -L -A \"{userAgent}\" {url}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null) return (null, null);

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (string.IsNullOrEmpty(output)) return (null, null);

                // Split headers and body
                var parts = output.Split(new[] { "\r\n\r\n", "\n\n" }, 2, StringSplitOptions.None);
                var headerPart = parts[0];
                var html = parts.Length > 1 ? parts[1] : string.Empty;

                // Extract Cookie from headers
                var cookieLines = headerPart.Split('\n').Where(l => l.StartsWith("set-cookie:", StringComparison.OrdinalIgnoreCase));
                cookie = string.Join("; ", cookieLines.Select(l => l.Substring(11).Split(';')[0].Trim()));

                var document = new HtmlDocument();
                document.LoadHtml(html);

                // Use HtmlAgilityPack to find the element with 'data-stk' attribute
                var node = document.DocumentNode.SelectSingleNode("//*[@data-stk]");
                if (node == null)
                {
                    var htmlSnippet = html.Length > 500 ? html.Substring(0, 500) : html;
                    _logger.LogWarning($"APIService.MacroMicro_GetAuthorize|WARNING| Could not find element with 'data-stk' attribute via curl. HTML Snippet: {htmlSnippet}");
                    return (null, null);
                }

                var authorize = node.GetAttributeValue("data-stk", string.Empty);
                if (string.IsNullOrWhiteSpace(authorize))
                {
                    _logger.LogWarning("APIService.MacroMicro_GetAuthorize|WARNING| 'data-stk' attribute value is empty.");
                    return (null, null);
                }

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
                {
                    await Task.Delay(1000);
                    res = await MacroMicro_GetAuthorize();
                    if (string.IsNullOrWhiteSpace(res.Item1)
                    || string.IsNullOrWhiteSpace(res.Item2))
                    {
                        return null;
                    }
                }

                var url = $"https://en.macromicro.me/charts/data/{key}";
                var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "curl.exe",
                    Arguments = $"-s -L -A \"{userAgent}\" -H \"authorization: Bearer {res.Item1}\" -H \"cookie: {res.Item2}\" -H \"referer: https://en.macromicro.me/\" {url}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null) return null;

                var responseMessageStr = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (string.IsNullOrWhiteSpace(responseMessageStr)) return null;

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
