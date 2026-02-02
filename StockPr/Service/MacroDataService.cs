using HtmlAgilityPack;
using StockPr.Model.BCPT;
using StockPr.Parser;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IMacroDataService
    {
        Task<List<Pig333_Clean>> Pig333_GetPigPrice();
        Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities();
        Task<MacroMicro_Key> MacroMicro_WCI(string key);
        Task<List<Metal_Detail>> Metal_GetYellowPhotpho();
        Task<string> TongCucThongKeGetUrl();
        Task<Stream> TongCucThongKeGetFile(string url);
    }
    public class MacroDataService : IMacroDataService
    {
        private readonly ILogger<MacroDataService> _logger;
        private readonly IHttpClientFactory _client;
        private readonly IMacroParser _parser;
        public MacroDataService(ILogger<MacroDataService> logger, IHttpClientFactory httpClientFactory, IMacroParser parser)
        {
            _logger = logger;
            _client = httpClientFactory;
            _parser = parser;
        }

        public async Task<List<Pig333_Clean>> Pig333_GetPigPrice()
        {
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://www.pig333.com/markets_and_prices/?accio=cotitzacions");
                request.Headers.Add("user-agent", "zzz");
                var content = new StringContent("moneda=VND&unitats=kg&mercats=166", null, "application/x-www-form-urlencoded");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMessageStr = await response.Content.ReadAsStringAsync();
                return _parser.ParsePigPrice(responseMessageStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.Pig333_GetPigPrice|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities()
        {
            try
            {
                var lCode = new List<string>
                {
                    EPrice.Crude_Oil.GetDisplayName(),
                    EPrice.Natural_gas.GetDisplayName(),
                    EPrice.Coal.GetDisplayName(),
                    EPrice.Gold.GetDisplayName(),
                    EPrice.Steel.GetDisplayName(),
                    EPrice.HRC_Steel.GetDisplayName(),
                    EPrice.Rubber.GetDisplayName(),
                    EPrice.Coffee.GetDisplayName(),
                    EPrice.Rice.GetDisplayName(),
                    EPrice.Sugar.GetDisplayName(),
                    EPrice.Urea.GetDisplayName(),
                    EPrice.polyvinyl.GetDisplayName(),
                    EPrice.Nickel.GetDisplayName(),
                    EPrice.milk.GetDisplayName(),
                    EPrice.kraftpulp.GetDisplayName(),
                    EPrice.Cotton.GetDisplayName(),
                    EPrice.DAP.GetDisplayName()
                };

                var lResult = new List<TradingEconomics_Data>();
                var url = "https://www.tradingeconomics.com/commodities";
                var client = _client.CreateClient("ResilientClient");
                
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                requestMessage.Headers.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
                requestMessage.Headers.Add("Referer", "https://tradingeconomics.com/");
                requestMessage.Headers.Add("Upgrade-Insecure-Requests", "1");
                
                var responseMessage = await client.SendAsync(requestMessage);
                responseMessage.EnsureSuccessStatusCode();
                var html = await responseMessage.Content.ReadAsStringAsync();
                return _parser.ParseCommodities(html, lCode);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.Tradingeconimic_Commodities|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<(string, string)> MacroMicro_GetAuthorize()
        {
            try
            {
                var cookie = string.Empty;
                var url = "https://en.macromicro.me/";
                var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";

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

                var parts = output.Split(new[] { "\r\n\r\n", "\n\n" }, 2, StringSplitOptions.None);
                var headerPart = parts[0];
                var html = parts.Length > 1 ? parts[1] : string.Empty;

                var cookieLines = headerPart.Split('\n').Where(l => l.StartsWith("set-cookie:", StringComparison.OrdinalIgnoreCase));
                cookie = string.Join("; ", cookieLines.Select(l => l.Substring(11).Split(';')[0].Trim()));

                return _parser.ParseMacroMicroAuth(html, cookie);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.MacroMicro_GetAuthorize|EXCEPTION| {ex.Message}");
            }
            return (null, null);
        }

        public async Task<MacroMicro_Key> MacroMicro_WCI(string key)
        {
            try
            {
                var res = await MacroMicro_GetAuthorize();
                if (string.IsNullOrWhiteSpace(res.Item1) || string.IsNullOrWhiteSpace(res.Item2))
                    return null;

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

                return _parser.ParseMacroMicroData(responseMessageStr, key);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.MacroMicro_WCI|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Metal_Detail>> Metal_GetYellowPhotpho()
        {
            try
            {
                var dt = DateTime.Now;
                var dtStart = dt.AddMonths(-1);
                var end = $"{dt.Year}-{dt.Month.To2Digit()}-{dt.Day.To2Digit()}";
                var start = $"{dtStart.Year}-{dtStart.Month.To2Digit()}-{dtStart.Day.To2Digit()}";

                var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJjZWxscGhvbmUiOiIiLCJjb21wYW55X2lkIjowLCJjb21wYW55X3N0YXR1cyI6MCwiY3JlYXRlX2F0IjoxNzcwMDI0MzYwLCJlbWFpbCI6Im5ndXllbnBodTEzMTJAZ21haWwuY29tIiwiZW5fZW5kX3RpbWUiOjAsImVuX3JlZ2lzdGVyX3N0ZXAiOjIsImVuX3JlZ2lzdGVyX3RpbWUiOjE3MjY5MzExMjUsImVuX3N0YXJ0X3RpbWUiOjAsImVuX3VzZXJfdHlwZSI6MCwiZW5kX3RpbWUiOjAsImlzX21haWwiOjAsImlzX3Bob25lIjowLCJsYW5ndWFnZSI6IiIsImx5X2VuZF90aW1lIjowLCJseV9zdGFydF90aW1lIjowLCJseV91c2VyX3R5cGUiOjAsInJlZ2lzdGVyX3RpbWUiOjE3MjY5MzExMjQsInN0YXJ0X3RpbWUiOjAsInVuaXF1ZV9pZCI6ImZiNzA2MWY5MTY3OGRiMWVmMmE0MDhiNzZhM2JmZGI1IiwidXNlcl9pZCI6Mzg2Mzk0MywidXNlcl9sYW5ndWFnZSI6ImNuIiwidXNlcl9uYW1lIjoiU01NMTcyNjkzMTEyNUd3IiwidXNlcl90eXBlIjowLCJ6eF9lbmRfdGltZSI6MCwienhfc3RhcnRfdGltZSI6MCwienhfdXNlcl90eXBlIjowfQ.KwZrAQPdXUBio3aEmX-avCecyLQYVjxJiSrekvXmZGM";
                var url = $"https://platform.metal.com/spotoverseascenter/v1/product_info/history/202005210065?product_id=202005210065&begin_date={start}&end_date={end}&line_type=d&shfe_line_type=time_sharing&auth_token={token}&language=en";
                
                var client = _client.CreateClient("ResilientClient");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36");
                request.Headers.Add("Referer", "https://www.metal.com/");
                request.Headers.Add("Origin", "https://www.metal.com");
                request.Headers.Add("source-type", "pc");
                
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMessageStr = await response.Content.ReadAsStringAsync();
                return _parser.ParseMetalPhoto(responseMessageStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.Metal_GetYellowPhotpho|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<string> TongCucThongKeGetUrl()
        {
            try
            {
                var lLink = new List<string>();
                var url = "https://www.nso.gov.vn/bao-cao-tinh-hinh-kinh-te-xa-hoi-hang-thang/?paged=1";
                var client = _client.CreateClient("ResilientClient");
                var html = await client.GetStringAsync(url);
                var linkedPages = _parser.ParseTongCucThongKeLinks(html, DateTime.Now.Year);
                
                foreach (var item in linkedPages)
                {
                    var detailHtml = await client.GetStringAsync(item);
                    var excelLink = _parser.ParseTongCucThongKeExcelLink(detailHtml);
                    
                    if (!string.IsNullOrEmpty(excelLink)) return excelLink;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.TongCucThongKeGetUrl|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<Stream> TongCucThongKeGetFile(string url)
        {
            try
            {
                var client = _client.CreateClient("ResilientClient");
                return await client.GetStreamAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MacroDataService.TongCucThongKeGetFile|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
