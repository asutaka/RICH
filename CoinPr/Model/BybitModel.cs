namespace CoinPr.Model
{
    public class BybitSymbol
    {
        public BybitSymbolResult result { get; set; }
    }

    public class BybitSymbolResult
    {
        public List<BybitQuoteTokenResult> quoteTokenResult { get; set; }
    }

    public class BybitQuoteTokenResult
    {
        public string tokenId { get; set; }
        public List<BybitSymbolDetail> quoteTokenSymbols { get; set; }
    }

    public class BybitSymbolDetail
    {
        public string si { get; set; }
        public string tfn { get; set; }
    }
}
