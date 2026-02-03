using Newtonsoft.Json;
using StockPr.Model;
using StockPr.Model.BCPT;
using StockPr.Parser;
using System.Net;

namespace StockPr.Service
{
    public interface IScraperService
    {
        Task<(bool, List<DSC_Data>)> DSC_GetPost();
        Task<(bool, List<VNDirect_Data>)> VNDirect_GetPost(bool isIndustry);
        Task<(bool, List<MigrateAsset_Data>)> MigrateAsset_GetPost();
        Task<(bool, List<AGR_Data>)> Agribank_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> SSI_GetPost(bool isIndustry);
        Task<(bool, List<VCI_Content>)> VCI_GetPost();
        Task<(bool, List<VCBS_Data>)> VCBS_GetPost();
        Task<(bool, List<BCPT_Crawl_Data>)> BSC_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> MBS_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> PSI_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> FPTS_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> KBS_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> CafeF_GetPost();
        Task<List<string>> News_NguoiQuanSat();
        Task<News_KinhTeChungKhoan> News_KinhTeChungKhoan();
        Task<List<News_Raw>> News_NguoiDuaTin();
        Task<List<F319Model>> F319_Scout(string acc);
    }
    public class ScraperService : IScraperService
    {
        private readonly ILogger<ScraperService> _logger;
        private readonly IHttpClientFactory _client;
        private readonly IScraperParser _parser;
        public ScraperService(ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IScraperParser parser)
        {
            _logger = logger;
            _client = httpClientFactory;
            _parser = parser;
        }

        #region DSC
        private async Task<string> DSC_GetKey()
        {
            try
            {
                var url = $"https://www.dsc.com.vn/bao-cao-phan-tich";
                var client = _client.CreateClient("ResilientClient");
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var responseMessage = await client.GetAsync("");
                var html = await responseMessage.Content.ReadAsStringAsync();
                var index = html.IndexOf("\"buildId\":\"");
                if (index < 0) return string.Empty;
                
                index += 10;
                var indexLast = html.IndexOf(",", index);
                if (indexLast < 0) return string.Empty;
                
                var len = indexLast - index;
                return html.Substring(index, len).Replace("\"", "").Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.DSC_GetKey|EXCEPTION| {ex.Message}");
            }
            return string.Empty;
        }

        public async Task<(bool, List<DSC_Data>)> DSC_GetPost()
        {
            try
            {
                var key = await DSC_GetKey();
                if (string.IsNullOrWhiteSpace(key)) return (false, null);

                var url = $"https://www.dsc.com.vn/_next/data/{key}/vi/bao-cao-phan-tich/tat-ca-bao-cao.json?slug=tat-ca-bao-cao";
                var client = _client.CreateClient("ResilientClient");
                var responseMessage = await client.GetAsync(url);
                if (responseMessage.StatusCode != HttpStatusCode.OK) return (false, null);

                var content = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<DSC_Main>(content);
                return (true, responseModel?.pageProps?.dataCategory?.dataList?.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.DSC_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region VNDirect
        public async Task<(bool, List<VNDirect_Data>)> VNDirect_GetPost(bool isIndustry)
        {
            var url = isIndustry 
                ? "https://api-finfo.vndirect.com.vn/v4/news?q=newsType:industry_report~locale:VN~newsSource:VNDIRECT&sort=newsDate:desc~newsTime:desc&size=20"
                : "https://api-finfo.vndirect.com.vn/v4/news?q=newsType:company_report~locale:VN~newsSource:VNDIRECT&sort=newsDate:desc~newsTime:desc&size=20";
            
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode != HttpStatusCode.OK) return (false, null);

                var content = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VNDirect_Main>(content);
                return (true, responseModel?.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.VNDirect_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region MigrateAsset
        public async Task<(bool, List<MigrateAsset_Data>)> MigrateAsset_GetPost()
        {
            var url = "https://masvn.com/api/categories/fe/56/article?paging=1&sort=published_at&direction=desc&active=1&page=1&limit=10";
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode != HttpStatusCode.OK) return (false, null);

                var content = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<MigrateAsset_Main>(content);
                return (true, responseModel?.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.MigrateAsset_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region Agribank
        public async Task<(bool, List<AGR_Data>)> Agribank_GetPost(bool isIndustry)
        {
            var cat = isIndustry ? 2 : 1;
            var dt = DateTime.Now;
            var cur = $"{dt.Year}/{dt.Month}/{dt.Day}";
            var next = $"{dt.AddDays(1).Year}/{dt.AddDays(1).Month}/{dt.AddDays(1).Day}";
            var url = $"https://agriseco.com.vn/api/Data/Report/SearchReports?categoryID={cat}&sourceID=5&sectorID=null&symbol=&keywords=&startDate={cur}&endDate={next}&startIndex=0&count=10";
            
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode != HttpStatusCode.OK) return (false, null);

                var content = await responseMessage.Content.ReadAsStringAsync();
                return (true, JsonConvert.DeserializeObject<List<AGR_Data>>(content));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.Agribank_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region SSI
        public async Task<(bool, List<BCPT_Crawl_Data>)> SSI_GetPost(bool isIndustry)
        {
            try
            {
                var url = isIndustry ? "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-nganh" : "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-cong-ty";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParseSSI(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.SSI_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region VCI
        public async Task<(bool, List<VCI_Content>)> VCI_GetPost()
        {
            var lResult = new List<VCI_Content>();
            var lEng = await VCI_GetPost_Lang(2);
            if (lEng.Item2 != null) lResult.AddRange(lEng.Item2);

            var lVi = await VCI_GetPost_Lang(1);
            if (lVi.Item2 != null) lResult.AddRange(lVi.Item2);

            return (true, lResult);
        }

        private async Task<(bool, List<VCI_Content>)> VCI_GetPost_Lang(int lang)
        {
            var url = $"https://www.vietcap.com.vn/api/cms-service/v1/page/analysis?is-all=true&page=0&size=10&direction=DESC&sortBy=date&language={lang}";
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode != HttpStatusCode.OK) return (false, null);

                var content = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VCI_Main>(content);
                return (true, responseModel.data.pagingGeneralResponses.content);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.VCI_GetPost_Lang|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region VCBS
        public async Task<(bool, List<VCBS_Data>)> VCBS_GetPost()
        {
            var url = "https://www.vcbs.com.vn/api/v1/ttpt-reports?limit=15&page=1&keyword=&locale=vi";
            try
            {
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode != HttpStatusCode.OK) return (false, null);

                var content = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VCBS_Main>(content);
                return (true, responseModel.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.VCBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region BSC
        public async Task<(bool, List<BCPT_Crawl_Data>)> BSC_GetPost(bool isIndustry)
        {
            try
            {
                var url = isIndustry ? "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/2" : "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/1";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParseBSC(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.BSC_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region MBS
        public async Task<(bool, List<BCPT_Crawl_Data>)> MBS_GetPost(bool isIndustry)
        {
            try
            {
                var url = isIndustry ? "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/bao-cao-phan-tich-nganh/" : "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/nghien-cuu-co-phieu/";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParseMBS(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.MBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region PSI
        public async Task<(bool, List<BCPT_Crawl_Data>)> PSI_GetPost(bool isIndustry)
        {
            try
            {
                var url = isIndustry ? "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-nganh" : "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-phan-tich-doanh-nghiep";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParsePSI(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.PSI_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region FPTS
        public async Task<(bool, List<BCPT_Crawl_Data>)> FPTS_GetPost(bool isIndustry)
        {
            try
            {
                var url = isIndustry ? "https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=174" : "https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=179";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParseFPTS(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.FPTS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region KBS
        public async Task<(bool, List<BCPT_Crawl_Data>)> KBS_GetPost(bool isIndustry)
        {
            try
            {
                var url = isIndustry ? "https://www.kbsec.com.vn/vi/bao-cao-nganh.htm" : "https://www.kbsec.com.vn/vi/bao-cao-cong-ty.htm";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParseKBS(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.KBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region CafeF
        public async Task<(bool, List<BCPT_Crawl_Data>)> CafeF_GetPost()
        {
            try
            {
                var url = "https://s.cafef.vn/phan-tich-bao-cao.chn";
                var client = _client.CreateClient("ResilientClient");
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                
                var responseMessage = await client.SendAsync(requestMessage);
                var html = await responseMessage.Content.ReadAsStringAsync();
                return (true, _parser.ParseCafeF(html));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.CafeF_GetPost|EXCEPTION| {ex.Message}");
                return (false, null);
            }
        }
        #endregion

        #region NEWS
        public async Task<List<string>> News_NguoiQuanSat()
        {
            try
            {
                var url = $"https://dulieu.nguoiquansat.vn/home/GetHeaderNews?_={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
                var client = _client.CreateClient("ResilientClient");
                var result = await client.GetStringAsync(url);
                var responseModel = JsonConvert.DeserializeObject<Money24h_NhomNganhResponse>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.News_NguoiQuanSat|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<News_KinhTeChungKhoan> News_KinhTeChungKhoan()
        {
            try
            {
                var url = "https://kinhtechungkhoan.vn/home/channel/970";
                var client = _client.CreateClient("ResilientClient");
                var result = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<News_KinhTeChungKhoan>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.News_KinhTeChungKhoan|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<News_Raw>> News_NguoiDuaTin()
        {
            var lRes = new List<News_Raw>();
            try
            {
                var url = "https://antt.nguoiduatin.vn/ajax/detail-box/205223.htm";
                var client = _client.CreateClient("ResilientClient");
                var html = await client.GetStringAsync(url);
                return _parser.ParseNguoiDuaTin(html);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.News_NguoiDuaTin|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        public async Task<List<F319Model>> F319_Scout(string acc)
        {
            var lOutput = new List<F319Model>();
            var url = $"https://f319.com/members/{acc}/recent-content?_xfNoRedirect=1&_xfResponseType=json";
            var referer = $"https://f319.com/members/{acc}/";
            try
            {
                var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "curl.exe",
                    Arguments = $"-s -L -A \"{userAgent}\" " +
                                $"-H \"accept: application/json, text/javascript, */*; q=0.01\" " +
                                $"-H \"accept-language: vi,en-US;q=0.9,en;q=0.8\" " +
                                $"-H \"priority: u=1, i\" " +
                                $"-H \"referer: {referer}\" " +
                                $"-H \"sec-ch-ua: \\\"Not(A:Brand\\\";v=\\\"8\\\", \\\"Chromium\\\";v=\\\"144\\\", \\\"Google Chrome\\\";v=\\\"144\\\"\" " +
                                $"-H \"sec-ch-ua-mobile: ?0\" " +
                                $"-H \"sec-ch-ua-platform: \\\"Windows\\\"\" " +
                                $"-H \"sec-fetch-dest: empty\" " +
                                $"-H \"sec-fetch-mode: cors\" " +
                                $"-H \"sec-fetch-site: same-origin\" " +
                                $"-H \"x-ajax-referer: {referer}\" " +
                                $"-H \"x-requested-with: XMLHttpRequest\" " +
                                $"\"{url}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null) return lOutput;

                var data = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (string.IsNullOrWhiteSpace(data)) return lOutput;

                var responseModel = JsonConvert.DeserializeObject<F319Raw>(data);
                if (responseModel != null)
                {
                    return _parser.ParseF319(responseModel.TemplateHtml);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ScraperService.F319_Scout|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }
        #endregion
    }
}
