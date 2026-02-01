using HtmlAgilityPack;
using Newtonsoft.Json;
using StockPr.Model;
using StockPr.Model.BCPT;

namespace StockPr.Parser
{
    public class MacroParser : IMacroParser
    {
        public List<Pig333_Clean> ParsePigPrice(string json)
        {
            var responseModel = JsonConvert.DeserializeObject<Pig333_Main>(json);
            if (responseModel?.resultat == null) return null;

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
                    if (decimal.TryParse(strSplit[3].Trim(), out var i3))
                    {
                        lRes.Add(new Pig333_Clean
                        {
                            Date = new DateTime(i0, i1 + 1, i2),
                            Value = i3
                        });
                    }
                }
                catch { }
            }
            return lRes.OrderBy(x => x.Date).ToList();
        }

        public List<TradingEconomics_Data> ParseCommodities(string html, List<string> lCode)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var lResult = new List<TradingEconomics_Data>();

            var tableNodes = doc.DocumentNode.SelectNodes("//table");
            if (tableNodes == null) return lResult;

            foreach (var item in tableNodes)
            {
                var tbody = item.ChildNodes["tbody"];
                if (tbody == null) continue;

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
                        else if (i == 1)
                        {
                            if (decimal.TryParse(columnsArray[i].InnerText.Replace(",", "").Trim(), out var val))
                                model.Price = Math.Round(val, 2);
                        }
                        else if (i == 4)
                        {
                            if (decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val))
                                model.Weekly = val;
                        }
                        else if (i == 5)
                        {
                            if (decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val))
                                model.Monthly = val;
                        }
                        else if (i == 6)
                        {
                            if (decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val))
                                model.YTD = val;
                        }
                        else if (i == 7)
                        {
                            if (decimal.TryParse(columnsArray[i].InnerText.Replace("%", "").Trim(), out var val))
                                model.YoY = val;
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

        public (string Authorize, string Cookie) ParseMacroMicroAuth(string html, string setCookieHeader)
        {
            var index = html.IndexOf("data-stk=");
            if (index < 0) return (null, null);

            var sub = html.Substring(index + 10);
            var indexCut = sub.IndexOf("\"");
            if (indexCut < 0) return (null, null);

            var authorize = sub.Substring(0, indexCut);
            return (authorize, setCookieHeader);
        }

        public MacroMicro_Key ParseMacroMicroData(string json, string key)
        {
            var responseModel = JsonConvert.DeserializeObject<MacroMicro_Main>(json);
            return key.Equals("946") ? responseModel?.data.key2 : responseModel?.data.key;
        }

        public List<Metal_Detail> ParseMetalPhoto(string json)
        {
            var responseModel = JsonConvert.DeserializeObject<Metal_Main>(json);
            return responseModel?.data?.priceListList;
        }

        public List<string> ParseTongCucThongKeLinks(string html, int year)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var yearStr = year.ToString();
            return doc.DocumentNode.Descendants("a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Where(x => x.Contains(yearStr)
                            && (x.Contains("01") || x.Contains("02") || x.Contains("03") || x.Contains("04") || x.Contains("05") || x.Contains("06")
                                || x.Contains("07") || x.Contains("08") || x.Contains("09") || x.Contains("10") || x.Contains("11") || x.Contains("12")))
                .ToList();
        }

        public string ParseTongCucThongKeExcelLink(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.Descendants("a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .FirstOrDefault(x => x.Contains(".xlsx"));
        }
    }
}
