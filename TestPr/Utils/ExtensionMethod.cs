using Skender.Stock.Indicators;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TestPr.Model;

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

        public static string GetDisplayName(this Enum enumValue)
        {
            try
            {
                return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()
                            .GetName();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static (bool, DateTime) IsExistTopB(this List<QuoteEx> lData)
        {
            try
            {
                var lbb = lData.GetBollingerBands();
                var lCheck = lData.TakeLast(20);
                var count = lCheck.Count();
                Quote sig = null;
                for (int i = 4; i < count - 3; i++)
                {
                    var prev_1 = lCheck.ElementAt(i - 4);
                    var prev_2 = lCheck.ElementAt(i - 3);
                    var prev_3 = lCheck.ElementAt(i - 2);
                    var prev_4 = lCheck.ElementAt(i - 1);
                    var cur = lCheck.ElementAt(i);
                    var next_5 = lCheck.ElementAt(i + 1);
                    var next_6 = lCheck.ElementAt(i + 2);
                    var next_7 = lCheck.ElementAt(i + 3);
                    //check BB
                    var bb = lbb.First(x => x.Date == cur.Date);
                    if (cur.High > (decimal)(bb.UpperBand ?? 0))
                        return (false, DateTime.MinValue);

                    if (cur.Open > cur.Close //Nến đỏ
                        || cur.Close <= prev_1.Close
                        || cur.Close <= prev_2.Close
                        || cur.Close <= prev_3.Close
                        || cur.Close <= prev_4.Close
                        || cur.Close < next_5.Close
                        || cur.Close < next_6.Close
                        || cur.Close < next_7.Close)
                        continue;

                    sig = cur;
                }

                if (sig != null)
                    return (true, sig.Date);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, DateTime.MinValue);
        }

        public static (bool, DateTime) IsExistBotB(this List<Quote> lData)
        {
            try
            {
                var lbb = lData.GetBollingerBands();
                var lCheck = lData.TakeLast(20);
                var count = lCheck.Count();
                Quote sig = null;
                for (int i = 4; i < count - 3; i++)
                {
                    var prev_1 = lCheck.ElementAt(i - 4);
                    var prev_2 = lCheck.ElementAt(i - 3);
                    var prev_3 = lCheck.ElementAt(i - 2);
                    var prev_4 = lCheck.ElementAt(i - 1);
                    var cur = lCheck.ElementAt(i);
                    var next_5 = lCheck.ElementAt(i + 1);
                    var next_6 = lCheck.ElementAt(i + 2);
                    var next_7 = lCheck.ElementAt(i + 3);
                    //check BB
                    var bb = lbb.First(x => x.Date == cur.Date);
                    if (cur.Low < (decimal)(bb.LowerBand ?? 0))
                        return (false, DateTime.MinValue);

                    if (cur.Open < cur.Close //Nến xanh
                        || cur.Close >= prev_1.Close
                        || cur.Close >= prev_2.Close
                        || cur.Close >= prev_3.Close
                        || cur.Close >= prev_4.Close
                        || cur.Close > next_5.Close
                        || cur.Close > next_6.Close
                        || cur.Close > next_7.Close)
                        continue;

                    sig = cur;
                }

                if (sig != null)
                    return (true, sig.Date);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, DateTime.MinValue);
        }

        public static double GetAngle(this Quote val, Quote prev, int distance)
        {
            try
            {
                if (distance < 5)
                    return 0;

                var div = Math.Abs(val.Close - prev.Close);

                return Math.Acos(distance / Math.Sqrt((double)(div * div + distance * distance)));
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }
    }
}
