using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockPr.Model;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System.Net;
using System.Text;

namespace StockPr.Service
{
    public interface IAPIService
    {
        Task<Stream> GetChartImage(string body);

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

        Task<List<Pig333_Clean>> Pig333_GetPigPrice();
        Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities();
        Task<MacroMicro_Key> MacroMicro_WCI(string key);
        Task<List<Metal_Detail>> Metal_GetYellowPhotpho();

        Task<string> TongCucThongKeGetUrl();
        Task<Stream> TongCucThongKeGetFile(string url);

        Task<Stream> TuDoanhHSX(DateTime dt);

        Task<List<Quote>> SSI_GetDataStock(string code);
        Task<decimal> SSI_GetFinanceStock(string code);
        Task<decimal> SSI_GetFreefloatStock(string code);


        Task<List<Money24h_PTKTResponse>> Money24h_GetMaTheoChiBao(string chibao);
        Task<List<Money24h_ForeignResponse>> Money24h_GetForeign(EExchange mode, EMoney24hTimeType type);
        Task<Money24h_NhomNganhResponse> Money24h_GetNhomNganh(EMoney24hTimeType type);

        Task<ReportDataIDResponse> VietStock_CDKT_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(string body);
        Task<ReportDataIDResponse> VietStock_KQKD_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(string body);
        Task<ReportTempIDResponse> VietStock_CSTC_GetListTempID(string code);
        Task<TempDetailValue_CSTCResponse> VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(string body);
        Task<ReportDataIDResponse> VietStock_TM_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_TM_ByReportDataIds(string body);
        Task<IEnumerable<BCTCAPIResponse>> VietStock_GetDanhSachBCTC(string code, int page);

        Task<BCPT_Crawl_Data> VinaCapital_Portfolio(int mode = 0);
        Task<BCPT_Crawl_Data> DragonCapital_Portfolio();
        Task<List<BCPT_Crawl_Data>> VCBF_Portfolio();
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
                var url = "http://127.0.0.1:7801";//Local
                //var url = "https://export.highcharts.com";
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

        #region Báo cáo phân tích
        private async Task<string> DSC_GetKey()
        {
            try
            {
                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var url = $"https://www.dsc.com.vn/bao-cao-phan-tich";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var html = await responseMessage.Content.ReadAsStringAsync();
                var index = html.IndexOf("\"buildId\":\"");
                if(index < 0)
                {
                    return string.Empty;
                }
                index += 10;
                var indexLast = html.IndexOf(",", index);
                if(indexLast < 0)
                {
                    return string.Empty;
                }
                var len = indexLast - index;
                var val = html.Substring(index, len).Replace("\"", "").Trim();

                return val;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.DSC_GetKey|EXCEPTION| {ex.Message}");
            }
            return string.Empty;
        }
        public async Task<(bool, List<DSC_Data>)> DSC_GetPost()
        {
            
            try
            {
                var key = await DSC_GetKey();
                if(string.IsNullOrWhiteSpace(key))
                    return (false, null);

                var url = $"https://www.dsc.com.vn/_next/data/{key}/vi/bao-cao-phan-tich/tat-ca-bao-cao.json?slug=tat-ca-bao-cao";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                    return (false, null);

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<DSC_Main>(responseMessageStr);
                return (true, responseModel?.pageProps?.dataCategory?.dataList?.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.DSC_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<VNDirect_Data>)> VNDirect_GetPost(bool isIndustry)
        {
            var url = $"https://api-finfo.vndirect.com.vn/v4/news?q=newsType:company_report~locale:VN~newsSource:VNDIRECT&sort=newsDate:desc~newsTime:desc&size=20";
            if (isIndustry)
            {
                url = $"https://api-finfo.vndirect.com.vn/v4/news?q=newsType:industry_report~locale:VN~newsSource:VNDIRECT&sort=newsDate:desc~newsTime:desc&size=20";
            }
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                    return (false, null);

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VNDirect_Main>(responseMessageStr);
                return (true, responseModel?.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VNDirect_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<MigrateAsset_Data>)> MigrateAsset_GetPost()
        {
            var url = $"https://masvn.com/api/categories/fe/56/article?paging=1&sort=published_at&direction=desc&active=1&page=1&limit=10";
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                    return (false, null);

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<MigrateAsset_Main>(responseMessageStr);
                return (true, responseModel?.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.MigrateAsset_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<AGR_Data>)> Agribank_GetPost(bool isIndustry)
        {
            var cat = 1;
            if (isIndustry)
            {
                cat = 2;
            }
            var dt = DateTime.Now;
            var cur = $"{dt.Year}/{dt.Month}/{dt.Day}";
            var dtNext = dt.AddDays(1);
            var next = $"{dtNext.Year}/{dtNext.Month}/{dtNext.Day}";
            var url = $"https://agriseco.com.vn/api/Data/Report/SearchReports?categoryID={cat}&sourceID=5&sectorID=null&symbol=&keywords=&startDate={cur}&endDate={next}&startIndex=0&count=10";
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                    return (false, null);

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<List<AGR_Data>>(responseMessageStr);
                return (true, responseModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Agribank_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> SSI_GetPost(bool isIndustry)
        {
            try
            {
                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var url = $"https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-cong-ty";
                if (isIndustry)
                {
                    url = "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-nganh";
                }
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                //var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                for (int i = 0; i < 15; i++)
                {
                    var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/main/section[2]/div/div[2]/div[2]/div/div[2]/div[{i + 1}]/div[1]/a");
                    var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/main/section[2]/div/div[2]/div[2]/div/div[2]/div[{i + 1}]/div[2]/p/span");
                    var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                    var timeStr = nodeTime?.InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(timeStr))
                        continue;

                    var strSplit = timeStr.Split('/');
                    if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                    {
                        var year = int.Parse(strSplit[2].Trim());
                        var month = int.Parse(strSplit[1].Trim());
                        var day = int.Parse(strSplit[0].Trim());
                        lResult.Add(new BCPT_Crawl_Data
                        {
                            id = $"{strSplit[2].Trim()}{strSplit[1].Trim()}{strSplit[0].Trim()}{title.Substring(0, 3)}",
                            title = title,
                            date = new DateTime(year, month, day)
                        });
                    }
                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.SSI_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<VCI_Content>)> VCI_GetPost()
        {
            var lResult = new List<VCI_Content>();
            var lEng = await VCI_GetPost_Lang(2);
            if (lEng.Item2?.Any() ?? false)
            {
                lResult.AddRange(lEng.Item2);
            }

            var lVi = await VCI_GetPost_Lang(1);
            if (lVi.Item2?.Any() ?? false)
            {
                lResult.AddRange(lVi.Item2);
            }

            return (true, lResult);
        }

        private async Task<(bool, List<VCI_Content>)> VCI_GetPost_Lang(int lang)
        {
            var url = $"https://www.vietcap.com.vn/api/cms-service/v1/page/analysis?is-all=true&page=0&size=10&direction=DESC&sortBy=date&language={lang}";
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                    return (false, null);

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VCI_Main>(responseMessageStr);
                return (true, responseModel.data.pagingGeneralResponses.content);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VCI_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<VCBS_Data>)> VCBS_GetPost()
        {
            var url = $"https://www.vcbs.com.vn/api/v1/ttpt-reports?limit=15&page=1&keyword=&locale=vi";
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    return (false, null);

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VCBS_Main>(responseMessageStr);
                return (true, responseModel.data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VCBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> BSC_GetPost(bool isIndustry)
        {
            try
            {
                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var url = $"https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/1";
                if (isIndustry)
                {
                    url = "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/2";
                }
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                //var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                for (int i = 0; i < 10; i++)
                {
                    var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[4]/div[4]/div[2]/div/table/tbody/tr[{i + 1}]/td[2]/a");
                    var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[4]/div[4]/div[2]/div/table/tbody/tr[{i + 1}]/td[1]");
                    var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                    var path = nodeCode?.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains("bao-cao-phan-tich")).Trim();
                    var timeStr = nodeTime?.InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(timeStr))
                        continue;

                    var strSplit = timeStr.Split('/');
                    if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                    {
                        var year = int.Parse(strSplit[2].Trim());
                        var month = int.Parse(strSplit[1].Trim());
                        var day = int.Parse(strSplit[0].Trim());
                        lResult.Add(new BCPT_Crawl_Data
                        {
                            id = $"{strSplit[2].Trim()}{strSplit[1].Trim()}{strSplit[0].Trim()}{title.Substring(0, 7).Trim()}",
                            title = title,
                            date = new DateTime(year, month, day),
                            path = $"https://www.bsc.com.vn{path}"
                        });
                    }
                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.BSC_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> MBS_GetPost(bool isIndustry)
        {
            try
            {
                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var url = $"https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/nghien-cuu-co-phieu/";
                if (isIndustry)
                {
                    url = "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/bao-cao-phan-tich-nganh/";
                }
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                for (int i = 0; i < 10; i++)
                {
                    var nodeCode = doc.DocumentNode.SelectSingleNode($"//*[@id=\"content\"]/div/div/div[2]/main/section[2]/div/div[1]/div[{i + 1}]/div/a");
                    var nodeTime = doc.DocumentNode.SelectSingleNode($"//*[@id=\"content\"]/div/div/div[2]/main/section[2]/div/div[1]/div[{i + 1}]/div/div[1]");
                    var nodePath = doc.DocumentNode.SelectSingleNode($"//*[@id=\"content\"]/div/div/div[2]/main/section[2]/div/div[1]/div[{i + 1}]/div/div[2]/a");
                    var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                    var timeStr = nodeTime?.InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(timeStr))
                        continue;

                    var path = nodePath.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains(".pdf")).Trim();
                    var strSplit = timeStr.Split('/');
                    if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                    {
                        var year = int.Parse(strSplit[2].Trim());
                        var month = int.Parse(strSplit[1].Trim());
                        var day = int.Parse(strSplit[0].Trim());
                        lResult.Add(new BCPT_Crawl_Data
                        {
                            id = $"{strSplit[2].Trim()}{strSplit[1].Trim()}{strSplit[0].Trim()}{title.Substring(0, 3)}",
                            title = title,
                            date = new DateTime(year, month, day),
                            path = path
                        });
                    }
                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.MBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> PSI_GetPost(bool isIndustry)
        {
            try
            {
                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var url = $"https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-phan-tich-doanh-nghiep";
                if (isIndustry)
                {
                    url = "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-nganh";
                }
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                for (int i = 0; i < 10; i++)
                {
                    var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[{i + 1}]/div[2]/div[1]/div[1]");
                    var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[{i + 1}]/div[1]/div/div[1]");
                    var nodePath = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[{i + 1}]/div[3]/div/a");
                    var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                    var timeStr = nodeTime?.InnerText.Trim().Replace("\n", "/");
                    if (string.IsNullOrWhiteSpace(timeStr))
                        continue;

                    var path = nodePath.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains(".pdf")).Trim();
                    var strSplit = timeStr.Split('/');
                    if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                    {
                        var year = int.Parse(strSplit[2].Trim());
                        var month = int.Parse(strSplit[1].Trim());
                        var day = int.Parse(strSplit[0].Trim());
                        lResult.Add(new BCPT_Crawl_Data
                        {
                            id = $"{strSplit[2].Trim()}{strSplit[1].Trim()}{strSplit[0].Trim()}{title.Substring(0, 3)}",
                            title = title,
                            date = new DateTime(year, month, day),
                            path = path
                        });
                    }
                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.PSI_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> FPTS_GetPost(bool isIndustry)
        {
            try
            {
                var url = $"https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=179";
                if (isIndustry)
                {
                    url = $"https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=174";
                }

                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var lNode = doc.DocumentNode.SelectNodes($"//*[@id=\"grid\"]")?.Nodes();
                if (!lNode.Any())
                    return (false, null);

                foreach (var item in lNode)
                {
                    try
                    {
                        if (item.Name != "tr" || item.InnerText.Trim().Length < 100)
                            continue;
                        var nodeTime = item.ChildNodes[1];
                        var nodeCode = item.ChildNodes[2];
                        var nodeTitle = item.ChildNodes[3];

                        var title = nodeTitle?.InnerText.Replace("\n", "").Trim();
                        var code = nodeCode?.InnerText.Trim();
                        var timeStr = nodeTime?.InnerText.Trim().Replace("\n", "/");
                        if (string.IsNullOrWhiteSpace(timeStr))
                            continue;

                        var path = nodeTitle.OuterHtml.Split("'").FirstOrDefault(x => x.Contains(".pdf")).Trim();
                        var strSplit = timeStr.Split('/');
                        if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                        {
                            var year = int.Parse(strSplit[2].Trim());
                            var month = int.Parse(strSplit[1].Trim());
                            var day = int.Parse(strSplit[0].Trim());
                            title = $"{code} {title}";

                            lResult.Add(new BCPT_Crawl_Data
                            {
                                id = $"{strSplit[2].Trim()}{strSplit[1].Trim()}{strSplit[0].Trim()}{title.Substring(0, 3)}",
                                title = title,
                                date = new DateTime(year, month, day),
                                path = path
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"APIService.FPTS_GetPost|EXCEPTION(DETAIL)| INPUT: {JsonConvert.SerializeObject(item)}| {ex.Message}");
                    }

                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.FPTS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> KBS_GetPost(bool isIndustry)
        {
            try
            {
                var url = $"https://www.kbsec.com.vn/vi/bao-cao-cong-ty.htm";
                if (isIndustry)
                {
                    url = $"https://www.kbsec.com.vn/vi/bao-cao-nganh.htm";
                }

                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var lNode = doc.DocumentNode.SelectNodes($"//*[@id=\"form1\"]/main/div/div/div/div[2]/div[2]")?.Nodes();
                if (!lNode.Any())
                    return (false, null);

                foreach (var item in lNode)
                {
                    try
                    {
                        if (item.Name != "div" || item.InnerText.Trim().Length < 100)
                            continue;
                        var node = item.ChildNodes[1];

                        var title = node.InnerText.Trim();
                        var path = node.InnerHtml.Split("'").FirstOrDefault(x => x.Contains(".pdf"));
                        if (string.IsNullOrWhiteSpace(title)
                            || string.IsNullOrWhiteSpace(path))
                            continue;

                        var timeStr = item.ChildNodes[3].InnerText.Replace("AM", "").Replace("PM", "").Trim();
                        var dt = timeStr.ToDateTime("dd/MM/yyyy HH:mm:ss");

                        lResult.Add(new BCPT_Crawl_Data
                        {
                            id = $"{dt.Year}{dt.Month}{dt.Day}{path.Split('/').Last().Substring(0, 7)}",
                            title = title,
                            date = dt,
                            path = path,
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"APIService.KBS_GetPost|EXCEPTION(DETAIL)| INPUT: {JsonConvert.SerializeObject(item)}| {ex.Message}");
                    }

                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.KBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public async Task<(bool, List<BCPT_Crawl_Data>)> CafeF_GetPost()
        {
            try
            {
                var lResult = new List<BCPT_Crawl_Data>();
                var link = string.Empty;
                var url = $"https://s.cafef.vn/phan-tich-bao-cao.chn";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                for (int i = 0; i < 10; i++)
                {
                    var nodeCode = doc.DocumentNode.SelectSingleNode($"//*[@id=\"ContentPlaceHolder1_AnalyzeReportList1_rptData_itemTR_{i}\"]/td[2]/a");
                    var nodeTime = doc.DocumentNode.SelectSingleNode($"//*[@id=\"ContentPlaceHolder1_AnalyzeReportList1_rptData_itemTR_{i}\"]/td[1]");
                    var nodePath = doc.DocumentNode.SelectSingleNode($"//*[@id=\"ContentPlaceHolder1_AnalyzeReportList1_rptData_itemTR_{i}\"]/td[5]");
                    var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                    var timeStr = nodeTime?.InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(timeStr))
                        continue;

                    var path = nodePath.OuterHtml.Split("'").FirstOrDefault(x => x.Contains(".pdf")).Trim();
                    var strSplit = timeStr.Split('/');
                    if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                    {
                        var year = int.Parse(strSplit[2].Trim());
                        var month = int.Parse(strSplit[1].Trim());
                        var day = int.Parse(strSplit[0].Trim());
                        lResult.Add(new BCPT_Crawl_Data
                        {
                            id = $"{strSplit[2].Trim()}{strSplit[1].Trim()}{strSplit[0].Trim()}{title.Substring(0, 3)}",
                            title = title,
                            date = new DateTime(year, month, day),
                            path = $"https://cafef1.mediacdn.vn/Images/Uploaded/DuLieuDownload/PhanTichBaoCao/{path}"
                        });
                    }
                }

                return (true, lResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.CafeF_GetPost|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }
        #endregion

        #region Giá Ngành Hàng
        public async Task<List<Pig333_Clean>> Pig333_GetPigPrice()
        {
            try
            {
                var client = _client.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://www.pig333.com/markets_and_prices/?accio=cotitzacions");
                request.Headers.Add("user-agent", "zzz");
                var content = new StringContent("moneda=VND&unitats=kg&mercats=166", null, "application/x-www-form-urlencoded");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMessageStr = await response.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<Pig333_Main>(responseMessageStr);
                responseModel.resultat = responseModel.resultat.Where(x => x.Contains("economia.data.addRow")).ToList();
                responseModel.resultat = responseModel.resultat.Select(x => x.Replace("economia.data.addRow([new Date(", "").Replace("]", "").Replace(")", "")).ToList();

                var lRes = new List<Pig333_Clean>();
                foreach (var item in responseModel.resultat)
                {
                    try
                    {
                        var strSplit = item.Split(',');
                        var i0 = int.Parse(strSplit[0].Trim());
                        var i1 = int.Parse(strSplit[1].Trim());
                        var i2 = int.Parse(strSplit[2].Trim());
                        var isDecimal = decimal.TryParse(strSplit[3].Trim(), out var i3);
                        if (!isDecimal)
                            continue;
                        lRes.Add(new Pig333_Clean
                        {
                            Date = new DateTime(i0, i1 + 1, i2),
                            Value = i3
                        });
                    }
                    catch { }
                }
                lRes = lRes.OrderBy(x => x.Date).ToList();

                return lRes;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Pig333_GetPigPrice|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities()
        {
            try
            {
                var lCode = new List<string>
                {
                    EPrice.Crude_Oil.GetDisplayName(),//Dầu thô
                    EPrice.Natural_gas.GetDisplayName(),//Khí thiên nhiên
                    EPrice.Coal.GetDisplayName(),//Than
                    EPrice.Gold.GetDisplayName(),//Vàng--
                    EPrice.Steel.GetDisplayName(),//Thép
                    EPrice.HRC_Steel.GetDisplayName(),//Thép HRC
                    EPrice.Rubber.GetDisplayName(), //Cao su
                    EPrice.Coffee.GetDisplayName(), //Cà phê
                    EPrice.Rice.GetDisplayName(), //Gạo
                    EPrice.Sugar.GetDisplayName(), //Đường
                    EPrice.Urea.GetDisplayName(), //U rê
                    EPrice.polyvinyl.GetDisplayName(), //Ống nhựa PVC--
                    EPrice.Nickel.GetDisplayName(), //Niken
                    EPrice.milk.GetDisplayName(),//Sữa
                    EPrice.kraftpulp.GetDisplayName(),//Bột giấy
                    EPrice.Cotton.GetDisplayName(),//Bông
                };

                var lResult = new List<TradingEconomics_Data>();

                //LV1
                var link = string.Empty;
                var url = $"https://tradingeconomics.com/commodities";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);

                var requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Get;
                var responseMessage = await client.SendAsync(requestMessage);

                //var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var tableNodes = doc.DocumentNode.SelectNodes("//table");
                foreach (var item in tableNodes)
                {
                    var tbody = item.ChildNodes["tbody"];
                    foreach (var row in tbody.ChildNodes.Where(r => r.Name == "tr"))
                    {
                        var model = new TradingEconomics_Data();
                        var columnsArray = row.ChildNodes.Where(c => c.Name == "td").ToArray();
                        for (int i = 0; i < columnsArray.Length; i++)
                        {
                            if (i == 0)
                            {
                                model.Code = columnsArray[i].InnerText.Trim().Split("\r")[0].Trim();
                            }
                            else if (i == 4)
                            {
                                var isFloat = decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val);
                                if (isFloat)
                                {
                                    model.Weekly = val;
                                }
                            }
                            else if (i == 5)
                            {
                                var isFloat = decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val);
                                if (isFloat)
                                {
                                    model.Monthly = val;
                                }
                            }
                            else if (i == 6)
                            {
                                var isFloat = decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val);
                                if (isFloat)
                                {
                                    model.YTD = val;
                                }
                            }
                            else if (i == 7)
                            {
                                var isFloat = decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val);
                                if (isFloat)
                                {
                                    model.YoY = val;
                                }
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(model.Code)
                            && lCode.Any(x => x.Replace(" ", "").Replace("-", "").Equals(model.Code.Replace(" ", "").Replace("-", ""), StringComparison.OrdinalIgnoreCase)))
                        {
                            lResult.Add(model);
                        }
                    }
                }

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Tradingeconimic_Commodities|EXCEPTION| {ex.Message}");
            }
            return null;
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

        public async Task<List<Metal_Detail>> Metal_GetYellowPhotpho()
        {
            try
            {
                var dt = DateTime.Now;
                var dtStart = dt.AddYears(-1);
                var end = $"{dt.Year}-{dt.Month.To2Digit()}-{dt.Day.To2Digit()}";
                var start = $"{dtStart.Year}-{dtStart.Month.To2Digit()}-{dtStart.Day.To2Digit()}";

                var url = $"https://www.metal.com/api/spotcenter/get_history_prices?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJjZWxscGhvbmUiOiIiLCJjb21wYW55X2lkIjowLCJjb21wYW55X3N0YXR1cyI6MCwiY3JlYXRlX2F0IjoxNzI4ODE0NDE2LCJlbWFpbCI6Im5ndXllbnBodTEzMTJAZ21haWwuY29tIiwiZW5fZW5kX3RpbWUiOjAsImVuX3JlZ2lzdGVyX3N0ZXAiOjIsImVuX3JlZ2lzdGVyX3RpbWUiOjE3MjY5MzExMjUsImVuX3N0YXJ0X3RpbWUiOjAsImVuX3VzZXJfdHlwZSI6MCwiZW5kX3RpbWUiOjAsImlzX21haWwiOjAsImlzX3Bob25lIjowLCJsYW5ndWFnZSI6IiIsImx5X2VuZF90aW1lIjowLCJseV9zdGFydF90aW1lIjowLCJseV91c2VyX3R5cGUiOjAsInJlZ2lzdGVyX3RpbWUiOjE3MjY5MzExMjQsInN0YXJ0X3RpbWUiOjAsInVuaXF1ZV9pZCI6ImZiNzA2MWY5MTY3OGRiMWVmMmE0MDhiNzZhM2JmZGI1IiwidXNlcl9pZCI6Mzg2Mzk0MywidXNlcl9sYW5ndWFnZSI6ImNuIiwidXNlcl9uYW1lIjoiU01NMTcyNjkzMTEyNUd3IiwidXNlcl90eXBlIjowLCJ6eF9lbmRfdGltZSI6MCwienhfc3RhcnRfdGltZSI6MCwienhfdXNlcl90eXBlIjowfQ.Cto8fQMsanSaEDjBWPNPSMMSX68AaQp8_5uLgnVUYXE&id=202005210065&beginDate={start}&endDate={end}&needQuote=0";
                var client = _client.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMessageStr = await response.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<Metal_Main>(responseMessageStr);
                return responseModel?.data?.priceListList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Metal_GetYellowPhotpho|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        #endregion

        public async Task<string> TongCucThongKeGetUrl()
        {
            try
            {
                var lLink = new List<string>();
                for (int i = 1; i <= 1; i++)
                {
                    var url = $"https://www.gso.gov.vn/bao-cao-tinh-hinh-kinh-te-xa-hoi-hang-thang/?paged={i}";
                    var client = _client.CreateClient();
                    client.BaseAddress = new Uri(url);
                    client.Timeout = TimeSpan.FromSeconds(15);
                    var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                    var html = await responseMessage.Content.ReadAsStringAsync();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var linkedPages = doc.DocumentNode.Descendants("a")
                                                      .Select(a => a.GetAttributeValue("href", null))
                                                      .Where(u => !string.IsNullOrWhiteSpace(u))
                                                      .Where(x => (x.Contains("2024") || x.Contains("2023"))
                                                                && (x.Contains("01") || x.Contains("02") || x.Contains("03") || x.Contains("04") || x.Contains("05") || x.Contains("06")
                                                                    || x.Contains("07") || x.Contains("08") || x.Contains("09") || x.Contains("10") || x.Contains("11") || x.Contains("12")));

                    lLink.AddRange(linkedPages);
                }
                //each link
                foreach (var item in lLink)
                {
                    var clientDetail = new HttpClient { BaseAddress = new Uri(item) };
                    clientDetail.Timeout = TimeSpan.FromSeconds(15);
                    var responseMessage = await clientDetail.GetAsync("", HttpCompletionOption.ResponseContentRead);
                    var html = await responseMessage.Content.ReadAsStringAsync();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var linkedPages = doc.DocumentNode.Descendants("a")
                                                      .Select(a => a.GetAttributeValue("href", null))
                                                      .Where(u => !string.IsNullOrWhiteSpace(u))
                                                      .Where(x => x.Contains(".xlsx"));
                    return linkedPages.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.TongCucThongKeGetUrl|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<Stream> TongCucThongKeGetFile(string url)
        {
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                    return null;
                return await responseMessage.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.TongCucThongKeGetFile|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<Stream> TuDoanhHSX(DateTime dt)
        {
            try
            {
                //LV1
                var link = string.Empty;
                var url = "https://www.hsx.vn";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var html = await responseMessage.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                link = doc.DocumentNode.Descendants("a")
                    .Where(x => x.InnerHtml.Contains("giao dịch tự doanh") && x.InnerHtml.Contains($"{dt.Day.To2Digit()}/{dt.Month.To2Digit()}/{dt.Year}"))
                                                      .Select(a => a.GetAttributeValue("href", null)).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(link))
                    return null;

                //LV2
                var clientDetail = new HttpClient { BaseAddress = new Uri($"{url}{link.Replace("ViewArticle", "GetRelatedFiles")}?rows=30&page=1") };
                responseMessage = await clientDetail.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var content = await responseMessage.Content.ReadAsStringAsync();
                var model = JsonConvert.DeserializeObject<HSXTudoanhModel>(content);
                var lastID = model.rows?.FirstOrDefault()?.cell?.FirstOrDefault();
                //LV3
                var clientDownload = new HttpClient { BaseAddress = new Uri($"{url}/Modules/CMS/Web/DownloadFile?id={lastID}") };
                responseMessage = await clientDownload.GetAsync("", HttpCompletionOption.ResponseContentRead);
                return await responseMessage.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.TuDoanhHSX|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Money24h_PTKTResponse>> Money24h_GetMaTheoChiBao(string chibao)
        {
            var lOutput = new List<Money24h_PTKTResponse>();
            try
            {
                var body = "{\"floor_codes\":[\"10\",\"11\",\"02\",\"03\"],\"group_ids\":[\"0001\",\"1000\",\"2000\",\"3000\",\"4000\",\"5000\",\"6000\",\"7000\",\"8000\",\"8301\",\"9000\"],\"signals\":[{\"" + chibao + "\":\"up\"}]}";
                var url = "https://api-finance-t19.24hmoney.vn/v2/web/indices/technical-signal-filter?sort=asc&page=1&per_page=50";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(5);
                var requestMessage = new HttpRequestMessage();
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var responseMessage = await client.SendAsync(requestMessage);
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<Money24h_PTKT_LV1Response>(resultArray);
                if (responseModel.status == 200
                    && responseModel.data.total_symbol > 0
                    && responseModel.data.data.Any())
                {
                    return responseModel.data.data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Money24h_GetMaTheoChiBao|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        public async Task<List<Money24h_ForeignResponse>> Money24h_GetForeign(EExchange mode, EMoney24hTimeType type)
        {
            var lOutput = new List<Money24h_ForeignResponse>();
            try
            {
                var urlBase = "https://api-finance-t19.24hmoney.vn/v2/web/indices/foreign-trading-all-stock-by-time?code={0}&type={1}";
                var url = string.Format(urlBase, mode.GetDisplayName(), type.GetDisplayName());
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<Money24h_ForeignAPIResponse>(resultArray);
                if (responseModel.status == 200
                    && responseModel.data.data.Any())
                {
                    var date = responseModel.data.from_date.ToDateTime("dd/MM/yyyy");
                    if (date.Day == DateTime.Now.Day)
                    {
                        return responseModel.data.data.Where(x => x.symbol.Length == 3).OrderByDescending(x => x.net_val).Select((x, index) => new Money24h_ForeignResponse
                        {
                            no = index + 1,
                            d = new DateTimeOffset(date, TimeSpan.FromHours(0)).ToUnixTimeSeconds(),
                            s = x.symbol,
                            sell_qtty = x.sell_qtty,
                            sell_val = x.sell_val,
                            buy_qtty = x.buy_qtty,
                            buy_val = x.buy_val,
                            net_val = x.net_val,
                            t = DateTimeOffset.Now.ToUnixTimeSeconds()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Money24h_GetForeign|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        public async Task<Money24h_NhomNganhResponse> Money24h_GetNhomNganh(EMoney24hTimeType type)
        {
            try
            {
                var urlBase = "https://api-finance-t19.24hmoney.vn/v2/ios/company-group/all-level-with-summary?type={0}";
                var url = string.Format(urlBase, type.GetDisplayName());
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var result = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<Money24h_NhomNganhResponse>(result);
                if (responseModel.status == 200)
                {
                    return responseModel;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Money24h_GetNhomNganh|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<Quote>> SSI_GetDataStock(string code)
        {
            var lOutput = new List<Quote>();
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/charts/history?symbol={0}&resolution={1}&from={2}&to={3}";
            try
            {
                var url = string.Format(urlBase, code, "1D", DateTimeOffset.Now.AddYears(-2).ToUnixTimeSeconds(), DateTimeOffset.Now.ToUnixTimeSeconds());
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<SSI_DataTradingResponse>(resultArray);
                if (responseModel.data.t.Any())
                {
                    var count = responseModel.data.t.Count();
                    for (int i = 0; i < count; i++)
                    {
                        lOutput.Add(new Quote
                        {
                            Date = responseModel.data.t.ElementAt(i).UnixTimeStampToDateTime(),
                            Open = responseModel.data.o.ElementAt(i),
                            Close = responseModel.data.c.ElementAt(i),
                            High = responseModel.data.h.ElementAt(i),
                            Low = responseModel.data.l.ElementAt(i),
                            Volume = responseModel.data.v.ElementAt(i)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.SSI_GetDataStock|EXCEPTION| {ex.Message}");
            }
            Thread.Sleep(200);
            return lOutput;
        }

        public async Task<decimal> SSI_GetFinanceStock(string code)
        {
            var lOutput = new List<Quote>();
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/company/financial-indicator?symbol={0}&page=1&pageSize=1";
            try
            {
                var url = string.Format(urlBase, code);
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                Thread.Sleep(100);
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<SSI_DataFinanceResponse>(resultArray);
                if (responseModel.data.Any())
                {
                    return responseModel.data.First().eps;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.SSI_GetFinanceStock|EXCEPTION| {ex.Message}");
            }
            return 0;
        }

        public async Task<decimal> SSI_GetFreefloatStock(string code)
        {
            var lOutput = new List<Quote>();
            var urlBase = "https://iboard-api.ssi.com.vn/statistics/company/shareholders?symbol={0}&language=vn&page=1&pageSize=100";
            try
            {
                var url = string.Format(urlBase, code);
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                Thread.Sleep(100);
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<SSI_ShareholderResponse>(resultArray);
                if (responseModel.data.Any())
                {
                    return Math.Round(100 * (1 - responseModel.data.Sum(x => x.percentage)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.SSI_GetFreeloadStock|EXCEPTION| {ex.Message}");
            }
            return 0;
        }

        #region Báo cáo tài chính
        public async Task<ReportDataIDResponse> VietStock_CDKT_GetListReportData(string code)
        {
            var url = "https://finance.vietstock.vn/data/CDKT_GetListReportData";
            return await VietStock_GetListReportData(code, url);
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetReportDataDetailValueByReportDataIds";
            return await VietStock_GetReportDataDetailValue(body, url);
        }

        public async Task<ReportDataIDResponse> VietStock_KQKD_GetListReportData(string code)
        {
            var url = "https://finance.vietstock.vn/data/KQKD_GetListReportData";
            return await VietStock_GetListReportData(code, url);
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetReportDataDetailValue_KQKD_ByReportDataIds";
            return await VietStock_GetReportDataDetailValue(body, url);
        }

        public async Task<ReportTempIDResponse> VietStock_CSTC_GetListTempID(string code)
        {
            var url = "https://finance.vietstock.vn/data/CSTC_GetListTerms";
            return await VietStock_GetListTempID(code, url);
        }

        public async Task<TempDetailValue_CSTCResponse> VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetFinanceIndexDataValue_CSTC_ByListTerms";
            return await GetFinanceIndexDataValue(body, url);
        }

        public async Task<ReportDataIDResponse> VietStock_TM_GetListReportData(string code)
        {
            var url = "https://finance.vietstock.vn/data/TM_GetListReportData";
            return await VietStock_GetListReportData(code, url);
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_TM_ByReportDataIds(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetReportDataDetailValue_TM_ByReportDataIds";
            return await VietStock_GetReportDataDetailValue(body, url);
        }

        public async Task<IEnumerable<BCTCAPIResponse>> VietStock_GetDanhSachBCTC(string code, int page)
        {
            var body = $"code={code}&page={page}&type=1&__RequestVerificationToken=9a3VgzLV-OF13vpUO4ZZZaYuc9nMSuYJluPAgu7mU-Rpn-c0c8j4tKvFHA1btqPWUexg4pMMYqLIU3bggz-O0xZkdR9thlg1ZwqGcU4ObPhTz8sh97k6mpGzwmeSdqHQ0";
            try
            {
                var url = "https://finance.vietstock.vn/data/getdocument";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var requestMessage = new HttpRequestMessage();
                //requestMessage.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                requestMessage.Headers.Add("Cookie", "ASP.NET_SessionId=kez23nkspuoomciouahd1xqp; __RequestVerificationToken=5t0qgD3M2IWZKLXukNsWaFE2ZCWl_cKVOn2SDHUDDw6NIEfBM1FC1HWEnrE9BzsrKeZrRWbGyYItV21WS4E6t-CTsKZqRvQIv6Ma5qAegwU1; language=vi-VN; _ga=GA1.1.1323687995.1720524498; _ga_EXMM0DKVEX=GS1.1.1720524497.1.0.1720524497.60.0.0; vts_usr_lg=A48AA659415FEE16F7CD0976F49923629A486E7AD4073A0F7F92268AEC798D2599F993737255E9990209E28582ABC797C68C46C2209B505875B2542FB92A23DCEFB76C9610DA504D7C120024CAB560DA51EC06B2D17034BFA7F517529B3FF3340438A76004E762194D4CAC1C45600B90CF3FF9475AB984756A4F22DC52765B59; finance_viewedstock=ACB,; Theme=Light");
                requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var responseMessage = await client.SendAsync(requestMessage);
                var resultArray = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<IEnumerable<BCTCAPIResponse>>(resultArray);
                return responseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VietStock_GetDanhSachBCTC|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<ReportDataIDResponse> VietStock_GetListReportData(string code, string url)
        {
            try
            {
                var body = $"StockCode={code}&UnitedId=-1&AuditedStatusId=-1&Unit=1000000000&IsNamDuongLich=false&PeriodType=QUY&SortTimeType=Time_ASC&__RequestVerificationToken={StaticVal._VietStock_Token}";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Add("Cookie", StaticVal._VietStock_Cookie);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Mobile Safari/537.36");
                var requestMessage = new HttpRequestMessage();
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseStr = await responseMessage.Content.ReadAsStringAsync();
                    if (responseStr.Contains("<!DOCTYPE"))
                        return null;

                    var responseModel = JsonConvert.DeserializeObject<ReportDataIDResponse>(responseStr);
                    return responseModel;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VietStock_GetListReportData|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<ReportTempIDResponse> VietStock_GetListTempID(string code, string url)
        {
            try
            {
                var body = $"StockCode={code}&UnitedId=-1&AuditedStatusId=-1&Unit=1000000000&IsNamDuongLich=false&PeriodType=QUY&SortTimeType=Time_ASC&__RequestVerificationToken={StaticVal._VietStock_Token}";
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Add("Cookie", StaticVal._VietStock_Cookie);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Mobile Safari/537.36");
                var requestMessage = new HttpRequestMessage();
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseStr = await responseMessage.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<ReportTempIDResponse>(responseStr);
                    return responseModel;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VietStock_GetListTempID|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue(string body, string url)
        {
            try
            {

                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Add("Cookie", StaticVal._VietStock_Cookie);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Mobile Safari/537.36");
                var requestMessage = new HttpRequestMessage();
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var responseStr = await responseMessage.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<ReportDataDetailValue_BCTTResponse>(responseStr);
                    return responseModel;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VietStock_GetReportDataDetailValue|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<TempDetailValue_CSTCResponse> GetFinanceIndexDataValue(string body, string url)
        {
            try
            {

                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Add("Cookie", StaticVal._VietStock_Cookie);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Mobile Safari/537.36");
                var requestMessage = new HttpRequestMessage();
                requestMessage.Method = HttpMethod.Post;
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var responseMessage = await client.SendAsync(requestMessage);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseStr = await responseMessage.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<TempDetailValue_CSTCResponse>(responseStr);
                    return responseModel;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.GetFinanceIndexDataValue|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        #endregion

        #region Portfolio
        public async Task<BCPT_Crawl_Data> VinaCapital_Portfolio(int mode = 0)
        {
            var url = $"https://vinacapital.com/vi/investment-solutions/onshore-funds/veof/";
            if(mode == 1)
            {
                url = $"https://vinacapital.com/vi/investment-solutions/onshore-funds/vesaf/";
            }
            else if(mode == 2)
            {
                url = $"https://vinacapital.com/vi/investment-solutions/onshore-funds/vmeef/";
            }

            var dt = DateTime.Now;
            var dFormat = $"{dt.Month.To2Digit()}/{dt.Year}";

            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("user-agent", "zz");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Thread.Sleep(200);
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var node = doc.DocumentNode.SelectNodes($"//*[@id=\"lsvvfff\"]")?.Nodes().FirstOrDefault();
                if (node is null)
                    return null;

                var title = node.InnerText.Trim();
                var path = node.InnerHtml.Split("\"").FirstOrDefault(x => x.Contains(".pdf"));
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(path))
                    return null;

                if (!title.Contains(dFormat))
                    return null;

                return new BCPT_Crawl_Data
                {
                    id = $"{dt.Year}{dt.Month.To2Digit()}",
                    title = title.Replace(dFormat, "").Replace("Mới","").Trim(),
                    date = dt,
                    path = path,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VinaCapital_Portfolio|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<BCPT_Crawl_Data> DragonCapital_Portfolio()
        {
            var lResult = new List<BCPT_Crawl_Data>();
            var url = $"https://www.veil-dragoncapital.com/fund/reports/";

            var dt = DateTime.Now;
            if(dt.Day <= 20)
            {
                dt = dt.AddMonths(-1);
            }
            string dFormat = $"{dt.Year}/{dt.Month.To2Digit()}";

            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("user-agent", "zz");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var link = doc.DocumentNode.Descendants("a")
                                                    .Select(a => a.GetAttributeValue("href", null))
                                                    .Where(u => !string.IsNullOrWhiteSpace(u));
                var path = link.FirstOrDefault(x => x.Contains(".pdf") && x.Contains(dFormat));
                if (path is null)
                    return null;

                return new BCPT_Crawl_Data
                {
                    id = $"{dt.Year}{dt.Month.To2Digit()}",
                    title = $"Dragon Capital - Tháng {dt.Month}",
                    date = dt,
                    path = path,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.PynElite_Portfolio|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> VCBF_Portfolio()
        {
            var lResult = new List<BCPT_Crawl_Data>();
            var url = $"https://www.vcbf.com/don-tai-lieu-quy/ban-thong-tin-quy/";

            var dt = DateTime.Now;
            if (dt.Month % 3 != 1)
                return null;

            dt = dt.AddMonths(-1);
            var dFormat = dt.ToString("MMM_yyyy").ToLower();

            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("user-agent", "zz");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var link = doc.DocumentNode.Descendants("a")
                                                    .Select(a => a.GetAttributeValue("href", null))
                                                    .Where(u => !string.IsNullOrWhiteSpace(u));
                var lPath = link.Where(x => x.Contains(".pdf") && x.Contains(dFormat));
                if (!lPath.Any())
                    return null;

                foreach (var item in lPath)
                {
                    var title = string.Empty;
                    if(item.Contains("tbf", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "QĐT Cân bằng chiến lược VCBF";
                    }
                    else if (item.Contains("mgf", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "QĐT Cổ phiếu tăng trưởng VCBF";
                    }
                    else if (item.Contains("fif", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "QĐT Trái phiếu VCBF";
                    }
                    else if (item.Contains("bcf", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "QĐT Cổ phiếu hàng đầu VCBF";
                    }

                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{dt.Year}{dt.Month}",
                        title = title,
                        date = dt,
                        path = $"https://www.vcbf.com/{item}"
                    });
                }

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.PynElite_Portfolio|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        #endregion
    }
}
