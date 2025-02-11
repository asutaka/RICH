namespace TestPr.Utils
{
    public static class ExtensionMethod
    {
        public static DateTime UnixTimeStampMinisecondToDateTime(this long unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public static DateTime longToDateTime(this long val)
        {
            var year = val / 10000;
            var day = val % 100;
            var month = (val % 10000) / 100;
            return new DateTime((int)year, (int)month, (int)day);
        }
    }
}
