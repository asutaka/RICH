namespace CoinPr.Utils
{
    public enum EUserMessageType
    {
        StockPr = 0,
        CoinPr = 1
    }

    public enum EExchange
    {
        Binance = 1,
        Bybit = 2
    }

    public enum EInterval
    {
        M15 = 1,//15m
        H1 = 2,//1h
        H4 = 3,//4h
        D1 = 4,//1d
        W1 = 5//1w
    }

    public enum EOrderBlockMode
    {
        TopPinbar = 1,
        TopInsideBar = 2,
        BotPinbar = 3,
        BotInsideBar = 4
    }
}
