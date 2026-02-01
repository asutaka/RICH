namespace StockPr.Parser
{
    public interface IMarketDataParser
    {
        string ParseTuDoanhHSXLink(string html, DateTime dt);
    }
}
