using System.ComponentModel.DataAnnotations;

namespace CoinUtilsPr
{
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

    public enum EExchange
    {
        Binance = 0,
        Bybit = 1
    }

    public enum EOption
    {
        Max = 1,// Giá trị lệnh 
        Thread = 2,//Số lệnh tối đa trong một thời điểm
        DisableAll = 3,//Tắt toàn bộ
        DisableLong = 4,//Tắt Long
        DisableShort = 5,//Tắt Short
    }

    public enum EOrderSideOption
    {
        OP_0 = 0,
        OP_1 = 1,
    }

    public enum EKey
    {
        Thread1 = 1,
        Thread2 = 2,
        Thread3 = 3,
        Thread4 = 4,
        Thread5 = 5,
        Binance = 6,
        Bybit = 7,
        Long = 8,
        Short = 9,
        Start = 11,
        Stop = 12,
        Add = 13,
        Delete = 14,
        Balance = 15,
        Max10 = 10,
        Max20 = 20,
        Max30 = 30,
        Max40 = 40,
        Max50 = 50,
        Max60 = 60,
        Max70 = 70,
        Max80 = 80,
        Max100 = 100,
        Max120 = 120,
        Max150 = 150,
        Max200 = 200,
        Max250 = 250,
        Max300 = 300,
        Max400 = 400,
        Max500 = 500,
        Max700 = 700,
        Max800 = 800,
        Max1000 = 1000,
    }
}
