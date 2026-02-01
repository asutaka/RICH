using HtmlAgilityPack;
using Newtonsoft.Json;
using StockPr.Model;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System.Net;

namespace StockPr.Parser
{
    public class ScraperParser : IScraperParser
    {
        public List<DSC_Data> ParseDSC(string html)
        {
            // Note: DSC_GetPost uses JSON now, so this might be for a different use or legacy.
            // But let's check ScraperService.DSC_GetKey()
            return null; 
        }

        public List<VNDirect_Data> ParseVNDirect(string html) => null; // JSON
        public List<MigrateAsset_Data> ParseMigrateAsset(string html) => null; // JSON
        public List<AGR_Data> ParseAgribank(string html) => null; // JSON

        public List<BCPT_Crawl_Data> ParseSSI(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lResult = new List<BCPT_Crawl_Data>();
            for (int i = 0; i < 15; i++)
            {
                var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/main/section[2]/div/div[2]/div[2]/div/div[2]/div[{i + 1}]/div[1]/a");
                var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/main/section[2]/div/div[2]/div[2]/div/div[2]/div[{i + 1}]/div[2]/p/span");
                var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                var timeStr = nodeTime?.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(timeStr)) continue;

                var strSplit = timeStr.Split('/');
                if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                {
                    var date = new DateTime(int.Parse(strSplit[2]), int.Parse(strSplit[1]), int.Parse(strSplit[0]));
                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{strSplit[2]}{strSplit[1]}{strSplit[0]}{title.Substring(0, 3)}",
                        title = title,
                        date = date
                    });
                }
            }
            return lResult;
        }

        public List<VCI_Content> ParseVCI(string html) => null; // JSON
        public List<VCBS_Data> ParseVCBS(string html) => null; // JSON

        public List<BCPT_Crawl_Data> ParseBSC(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lResult = new List<BCPT_Crawl_Data>();
            for (int i = 0; i < 10; i++)
            {
                var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[4]/div[4]/div[2]/div/table/tbody/tr[{i + 1}]/td[2]/a");
                var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[4]/div[4]/div[2]/div/table/tbody/tr[{i + 1}]/td[1]");
                var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                var path = nodeCode?.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains("bao-cao-phan-tich"))?.Trim();
                var timeStr = nodeTime?.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(timeStr)) continue;

                var strSplit = timeStr.Split('/');
                if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                {
                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{strSplit[2]}{strSplit[1]}{strSplit[0]}{title.Substring(0, 7).Trim()}",
                        title = title,
                        date = new DateTime(int.Parse(strSplit[2]), int.Parse(strSplit[1]), int.Parse(strSplit[0])),
                        path = $"https://www.bsc.com.vn{path}"
                    });
                }
            }
            return lResult;
        }

        public List<BCPT_Crawl_Data> ParseMBS(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lResult = new List<BCPT_Crawl_Data>();
            for (int i = 0; i < 10; i++)
            {
                var nodeCode = doc.DocumentNode.SelectSingleNode($"//*[@id=\"content\"]/div/div/div[2]/main/section[2]/div/div[1]/div[{i + 1}]/div/a");
                var nodeTime = doc.DocumentNode.SelectSingleNode($"//*[@id=\"content\"]/div/div/div[2]/main/section[2]/div/div[1]/div[{i + 1}]/div/div[1]");
                var nodePath = doc.DocumentNode.SelectSingleNode($"//*[@id=\"content\"]/div/div/div[2]/main/section[2]/div/div[1]/div[{i + 1}]/div/div[2]/a");
                var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                var timeStr = nodeTime?.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(timeStr)) continue;

                var path = nodePath?.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains(".pdf"))?.Trim();
                var strSplit = timeStr.Split('/');
                if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                {
                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{strSplit[2]}{strSplit[1]}{strSplit[0]}{title.Substring(0, 3)}",
                        title = title,
                        date = new DateTime(int.Parse(strSplit[2]), int.Parse(strSplit[1]), int.Parse(strSplit[0])),
                        path = path
                    });
                }
            }
            return lResult;
        }

        public List<BCPT_Crawl_Data> ParsePSI(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lResult = new List<BCPT_Crawl_Data>();
            for (int i = 0; i < 10; i++)
            {
                var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[{i + 1}]/div[2]/div[1]/div[1]");
                var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[{i + 1}]/div[1]/div/div[1]");
                var nodePath = doc.DocumentNode.SelectSingleNode($"/html/body/div[3]/div[3]/div[{i + 1}]/div[3]/div/a");
                var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                var timeStr = nodeTime?.InnerText.Trim().Replace("\n", "/");
                if (string.IsNullOrWhiteSpace(timeStr)) continue;

                var path = nodePath?.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains(".pdf"))?.Trim();
                var strSplit = timeStr.Split('/');
                if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                {
                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{strSplit[2]}{strSplit[1]}{strSplit[0]}{title.Substring(0, 3)}",
                        title = title,
                        date = new DateTime(int.Parse(strSplit[2]), int.Parse(strSplit[1]), int.Parse(strSplit[0])),
                        path = path
                    });
                }
            }
            return lResult;
        }

        public List<BCPT_Crawl_Data> ParseFPTS(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lNode = doc.DocumentNode.SelectNodes("//*[@id=\"grid\"]")?.Nodes();
            var lResult = new List<BCPT_Crawl_Data>();
            if (lNode == null) return lResult;

            foreach (var item in lNode)
            {
                if (item.Name != "tr" || item.InnerText.Trim().Length < 100) continue;
                var nodeTime = item.ChildNodes[1];
                var nodeCode = item.ChildNodes[2];
                var nodeTitle = item.ChildNodes[3];

                var title = nodeTitle?.InnerText.Replace("\n", "").Trim();
                var code = nodeCode?.InnerText.Trim();
                var timeStr = nodeTime?.InnerText.Trim().Replace("\n", "/");
                if (string.IsNullOrWhiteSpace(timeStr)) continue;

                var path = nodeTitle.OuterHtml.Split("'").FirstOrDefault(x => x.Contains(".pdf"))?.Trim();
                var strSplit = timeStr.Split('/');
                if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                {
                    var date = new DateTime(int.Parse(strSplit[2]), int.Parse(strSplit[1]), int.Parse(strSplit[0]));
                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{strSplit[2]}{strSplit[1]}{strSplit[0]}{(code + title).Substring(0, 3)}",
                        title = $"{code} {title}",
                        date = date,
                        path = path
                    });
                }
            }
            return lResult;
        }

        public List<BCPT_Crawl_Data> ParseKBS(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lNode = doc.DocumentNode.SelectNodes("//*[@id=\"form1\"]/main/div/div/div/div[2]/div[2]")?.Nodes();
            var lResult = new List<BCPT_Crawl_Data>();
            if (lNode == null) return lResult;

            foreach (var item in lNode)
            {
                if (item.Name != "div" || item.InnerText.Trim().Length < 100) continue;
                var node = item.ChildNodes[1];
                var title = node?.InnerText.Trim();
                var path = node?.InnerHtml?.Split("'")?.FirstOrDefault(x => x.Contains(".pdf"));
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(path)) continue;

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
            return lResult;
        }

        public List<BCPT_Crawl_Data> ParseCafeF(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lResult = new List<BCPT_Crawl_Data>();
            for (int i = 0; i < 20; i++)
            {
                var nodeCode = doc.DocumentNode.SelectSingleNode($"/html/body/form/div[3]/div/div[2]/div[2]/div[2]/div[1]/div[2]/table/tr[{i + 2}]/td[2]/a");
                var nodeTime = doc.DocumentNode.SelectSingleNode($"/html/body/form/div[3]/div/div[2]/div[2]/div[2]/div[1]/div[2]/table/tr[{i + 2}]/td[1]");
                var title = nodeCode?.InnerText.Replace("\n", "").Trim();
                var path = nodeCode?.OuterHtml.Split("\"").FirstOrDefault(x => x.Contains(".ashx"))?.Trim();
                var timeStr = nodeTime?.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(timeStr)) continue;

                var strSplit = timeStr.Split('/');
                if (strSplit.Length == 3 && !string.IsNullOrWhiteSpace(title))
                {
                    lResult.Add(new BCPT_Crawl_Data
                    {
                        id = $"{strSplit[2]}{strSplit[1]}{strSplit[0]}{title.Substring(0, 3)}",
                        title = title,
                        date = new DateTime(int.Parse(strSplit[2]), int.Parse(strSplit[1]), int.Parse(strSplit[0])),
                        path = $"https://s.cafef.vn{path}"
                    });
                }
            }
            return lResult;
        }

        public List<string> ParseNguoiQuanSat(string html) => null; // JSON actually? Service uses it but returns null.
        public News_KinhTeChungKhoan ParseKinhTeChungKhoan(string html) => null; // JSON
        
        public List<News_Raw> ParseNguoiDuaTin(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lRes = new List<News_Raw>();
            var categoryNodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'box-category-item')]");
            if (categoryNodes != null)
            {
                foreach (var node in categoryNodes)
                {
                    var linkNodes = node.SelectNodes(".//a");
                    if (linkNodes != null)
                    {
                        foreach (HtmlNode link in linkNodes)
                        {
                            var linkText = link.InnerText.Trim();
                            var linkHref = link.GetAttributeValue("href", "");
                            if (string.IsNullOrWhiteSpace(linkText)) continue;
                            lRes.Add(new News_Raw
                            {
                                ID = linkText.RemoveSignVietnamese().Replace(" ", "").Substring(0, Math.Min(linkText.Length, 60)),
                                LinktoMe2 = $"https://antt.nguoiduatin.vn{linkHref.Trim()}",
                                Title = WebUtility.HtmlDecode(linkText.Trim())
                            });
                            break;
                        }
                    }
                }
            }
            return lRes;
        }

        public List<F319Model> ParseF319(string templateHtml)
        {
            var lOutput = new List<F319Model>();
            var lSplit = templateHtml.Split(new string[] { "<div class=\"listBlock main\">" }, StringSplitOptions.None);
            if (lSplit.Length <= 1) return lOutput;

            lSplit = lSplit.Skip(1).ToArray();
            foreach (var item in lSplit)
            {
                try
                {
                    var model = new F319Model();
                    var indexH3_Start = item.IndexOf("<h3 class=\"title\">");
                    var indexH3_End = item.IndexOf("</h3>");
                    if (indexH3_Start < 0 || indexH3_End < 0) continue;

                    var titleStr = item.Substring(indexH3_Start + 18, indexH3_End - (indexH3_Start + 18));
                    var indexTitleStart = titleStr.IndexOf('"');
                    var indexTitleEnd = titleStr.IndexOf(">");
                    if (indexTitleStart < 0 || indexTitleEnd < 0) continue;

                    model.Url = titleStr.Substring(indexTitleStart + 1, indexTitleEnd - (indexTitleStart + 1));
                    model.Title = titleStr.Substring(indexTitleEnd + 1).Replace("</a>", "").Trim();

                    var indexBlockquoteStart = item.IndexOf("<blockquote class=\"snippet\">");
                    var indexBlockquoteEnd = item.IndexOf("</blockquote>");
                    if (indexBlockquoteStart >= 0 && indexBlockquoteEnd >= 0)
                    {
                        var contentStr = item.Substring(indexBlockquoteStart + 28, indexBlockquoteEnd - (indexBlockquoteStart + 28)).Trim();
                        var indexContentEnd = contentStr.IndexOf(">");
                        model.Content = indexContentEnd >= 0 ? contentStr.Substring(indexContentEnd + 1) : contentStr;
                    }

                    var indexTime = item.IndexOf("data-time=");
                    if (indexTime >= 0)
                    {
                        var timeStr = item.Substring(indexTime + 11, 10);
                        if (int.TryParse(timeStr, out int time))
                        {
                            model.TimePost = time;
                        }
                    }
                    lOutput.Add(model);
                }
                catch { }
            }
            return lOutput;
        }
    }
}
