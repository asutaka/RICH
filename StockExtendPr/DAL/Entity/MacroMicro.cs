﻿using MongoDB.Bson.Serialization.Attributes;

namespace StockExtendPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class MacroMicro : BaseDTO
    {
        public string s { get; set; }
        public string key { get; set; }
        public double W { get; set; }
        public double M { get; set; }
        public double Y { get; set; }
        public double YTD { get; set; }
        public double price { get; set; }
        public int t { get; set; }
    }
}
