using HtmlAgilityPack;
using Newtonsoft.Json;
using StockPr.Model;
using StockPr.Model.BCPT;
using StockPr.Parser;
using StockPr.Utils;
using System.Net;
using System.Text;

namespace StockPr.Service
{
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
                var web = new HtmlWeb { UseCookies = true };
                web.PostResponse += (request, response) =>
                {
                    cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                };
                var document = web.Load(url);
                var html = document.ParsedText;
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

                var client = _client.CreateClient("ResilientClient");
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://en.macromicro.me/charts/data/{key}");
                request.Headers.Add("authorization", $"Bearer {res.Item1}");
                request.Headers.Add("cookie", res.Item2);
                request.Headers.Add("referer", "https://en.macromicro.me/collections/22190/sun-ming-te-investment-dashboard/44756/drewry-world-container-index");
                request.Headers.Add("user-agent", "zzz");
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMessageStr = await response.Content.ReadAsStringAsync();
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
                var dtStart = dt.AddYears(-1);
                var end = $"{dt.Year}-{dt.Month.To2Digit()}-{dt.Day.To2Digit()}";
                var start = $"{dtStart.Year}-{dtStart.Month.To2Digit()}-{dtStart.Day.To2Digit()}";

                var url = $"https://www.metal.com/api/spotcenter/get_history_prices?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJjZWxscGhvbmUiOiIiLCJjb21wYW55X2lkIjowLCJjb21wYW55X3N0YXR1cyI6MCwiY3JlYXRlX2F0IjoxNzI4ODE0NDE2LCJlbWFpbCI6Im5ndXllbnBodTEzMTJAZ21haWwuY29tIiwiZW5fZW5kX3RpbWUiOjAsImVuX3JlZ2lzdGVyX3N0ZXAiOjIsImVuX3JlZ2lzdGVyX3RpbWUiOjE3MjY5MzExMjUsImVuX3N0YXJ0X3RpbWUiOjAsImVuX3VzZXJfdHlwZSI6MCwiZW5kX3RpbWUiOjAsImlzX21haWwiOjAsImlzX3Bob25lIjowLCJsYW5ndWFnZSI6IiIsImx5X2VuZF90aW1lIjowLCJseV9zdGFydF90aW1lIjowLCJseV91c2VyX3R5cGUiOjAsInJlZ2lzdGVyX3RpbWUiOjE3MjY5MzExMjQsInN0YXJ0X3RpbWUiOjAsInVuaXF1ZV9pZCI6ImZiNzA2MWY5MTY3OGRiMWVmMmE0MDhiNzZhM2JmZGI1IiwidXNlcl9pZCI6Mzg2Mzk0MywidXNlcl9sYW5ndWFnZSI6ImNuIiwidXNlcl9uYW1lIjoiU01NMTcyNjkzMTEyNUd3IiwidXNlcl90eXBlIjowLCJ6eF9lbmRfdGltZSI6MCwienhfc3RhcnRfdGltZSI6MCwienhfdXNlcl90eXBlIjowfQ.Cto8fQMsanSaEDjBWPNPSMMSX68AaQp8_5uLgnVUYXE&id=202005210065&beginDate={start}&endDate={end}&needQuote=0";
                var client = _client.CreateClient("ResilientClient");
                var responseMessageStr = await client.GetStringAsync(url);
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
