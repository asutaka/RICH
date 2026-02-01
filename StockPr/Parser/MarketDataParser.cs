using HtmlAgilityPack;
using StockPr.Utils;

namespace StockPr.Parser
{
    public class MarketDataParser : IMarketDataParser
    {
        public string ParseTuDoanhHSXLink(string html, DateTime dt)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var dateStr = $"{dt.Day.To2Digit()}/{dt.Month.To2Digit()}/{dt.Year}";
            
            return doc.DocumentNode.Descendants("a")
                .Where(x => x.InnerHtml.Contains("giao dịch tự doanh") && x.InnerHtml.Contains(dateStr))
                .Select(a => a.GetAttributeValue("href", null))
                .FirstOrDefault();
        }
    }
}
