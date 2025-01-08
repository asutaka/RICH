using HtmlAgilityPack;
using Newtonsoft.Json;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IAPIService
    {
        Task<Stream> GetChartImage(string body);

        Task<List<DSC_Data>> DSC_GetPost();
        Task<List<VNDirect_Data>> VNDirect_GetPost(bool isIndustry);
        Task<List<MigrateAsset_Data>> MigrateAsset_GetPost();
        Task<List<AGR_Data>> Agribank_GetPost(bool isIndustry);
        Task<List<BCPT_Crawl_Data>> SSI_GetPost(bool isIndustry);
        Task<List<VCI_Content>> VCI_GetPost();
        Task<List<VCBS_Data>> VCBS_GetPost();
        Task<List<BCPT_Crawl_Data>> BSC_GetPost(bool isIndustry);
        Task<List<BCPT_Crawl_Data>> MBS_GetPost(bool isIndustry);
        Task<List<BCPT_Crawl_Data>> PSI_GetPost(bool isIndustry);
        Task<List<BCPT_Crawl_Data>> FPTS_GetPost(bool isIndustry);
        Task<List<BCPT_Crawl_Data>> KBS_GetPost(bool isIndustry);
        Task<List<BCPT_Crawl_Data>> CafeF_GetPost();
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

        public async Task<List<DSC_Data>> DSC_GetPost()
        {
            var url = $"https://www.dsc.com.vn/_next/data/_yb_7FS7Rg1u71yUzTxPK/bao-cao-phan-tich/tat-ca-bao-cao.json?slug=tat-ca-bao-cao";
            try
            {
                var client = _client.CreateClient();
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromSeconds(15);
                var responseMessage = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<DSC_Main>(responseMessageStr);
                return responseModel?.pageProps?.dataCategory?.dataList?.data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.DSC_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<VNDirect_Data>> VNDirect_GetPost(bool isIndustry)
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

                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VNDirect_Main>(responseMessageStr);
                return responseModel?.data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VNDirect_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<MigrateAsset_Data>> MigrateAsset_GetPost()
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

                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<MigrateAsset_Main>(responseMessageStr);
                return responseModel?.data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.MigrateAsset_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<AGR_Data>> Agribank_GetPost(bool isIndustry)
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

                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<List<AGR_Data>>(responseMessageStr);
                return responseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.Agribank_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> SSI_GetPost(bool isIndustry)
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.SSI_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<VCI_Content>> VCI_GetPost()
        {
            var lResult = new List<VCI_Content>();
            var lEng = await VCI_GetPost_Lang(2);
            if (lEng?.Any() ?? false)
            {
                lResult.AddRange(lEng);
            }

            var lVi = await VCI_GetPost_Lang(1);
            if (lVi?.Any() ?? false)
            {
                lResult.AddRange(lVi);
            }

            return lResult;
        }

        private async Task<List<VCI_Content>> VCI_GetPost_Lang(int lang)
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

                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VCI_Main>(responseMessageStr);
                return responseModel.data.pagingGeneralResponses.content;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VCI_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<VCBS_Data>> VCBS_GetPost()
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
                    return null;

                var responseMessageStr = await responseMessage.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<VCBS_Main>(responseMessageStr);
                return responseModel.data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.VCBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> BSC_GetPost(bool isIndustry)
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.BSC_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> MBS_GetPost(bool isIndustry)
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.MBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> PSI_GetPost(bool isIndustry)
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.PSI_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> FPTS_GetPost(bool isIndustry)
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
                    return null;

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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.FPTS_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> KBS_GetPost(bool isIndustry)
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
                    return null;

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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.KBS_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<List<BCPT_Crawl_Data>> CafeF_GetPost()
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"APIService.CafeF_GetPost|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
