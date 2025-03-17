using System.ComponentModel.DataAnnotations;

namespace TradePr.Utils
{
    public enum ELiquidMode
    {
        MuaCungChieu = 1,
        MuaNguocChieu = 2,
        BanCungChieu = 3,
        BanNguocChieu = 4
    }

    public enum EInterval
    {
        [Display(Name = "15m")]
        M15 = 1,//15m
        [Display(Name = "1h")]
        H1 = 2,//1h
        [Display(Name = "4h")]
        H4 = 3,//4h
        [Display(Name = "1d")]
        D1 = 4,//1d
        [Display(Name = "1w")]
        W1 = 5,//1w
        [Display(Name = "1m")]
        M1 = 6,
        [Display(Name = "5m")]
        M5 = 7
    }

    public enum ETypeBot
    {
        TokenUnlock = 1,
        ThreeSignal = 2
    }

    public enum EAction
    {
        Long = 0,
        Short = 1,
        Short_SL = 2,
        GetPosition = 3
    }

    public enum EExchange
    {
        Binance = 0,
        Bybit = 1
    }

    public enum EOption
    {
        Unlock = 1,
        Signal = 2,
        ThreeSignal = 3,
        Liquid = 4,
        RSI = 5
    }
}
