namespace StockPr.Utils
{
    public static class ExtensionMethod
    {
        public static string RemoveSpace(this string val)
        {
            return val.Replace(" ", "").Replace(",", "").Replace(".", "").Replace("-", "").Replace("_", "").Trim();
        }

        public static long GetYoyQuarter(this int time)
        {
            var year = time / 10;
            var quarter = time - year * 10;
            return int.Parse($"{year - 1}{quarter}");
        }

        public static string GetNameQuarter(this int time)
        {
            var year = time / 10;
            var quarter = time - year * 10;
            return $"{quarter.To2Digit()}/{year - 2000}";
        }

        public static string To2Digit(this int val)
        {
            if (val > 9)
                return val.ToString();
            return $"0{val}";
        }
    }
}
