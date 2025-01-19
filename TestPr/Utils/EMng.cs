﻿using System.ComponentModel.DataAnnotations;

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
        [Display(Name = "M15")]
        M15 = 1,//15m
        [Display(Name = "H1")]
        H1 = 2,//1h
        [Display(Name = "H4")]
        H4 = 3,//4h
        [Display(Name = "D1")]
        D1 = 4,//1d
        [Display(Name = "W1")]
        W1 = 5//1w
    }
}
