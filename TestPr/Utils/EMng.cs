using System.ComponentModel.DataAnnotations;

namespace TestPr.Utils
{
    public enum EOrderBlockMode
    {
        TopPinbar = 1,
        TopInsideBar = 2,
        BotPinbar = 3,
        BotInsideBar = 4
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
}
