using Skender.Stock.Indicators;
using StockPr.DAL.Entity;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace StockPr.Utils
{
    public static class ExtensionMethod
    {
        public static string RemoveSpace(this string val)
        {
            return val.Replace(" ", "").Replace(",", "").Replace(".", "").Replace("-", "").Replace("_", "").Replace("(", "").Replace(")","").Trim();
        }

        public static double GetAngle(this double val, double prev, int distance)
        {
            try
            {
                var UNIT = 120;
                if (distance < 5)
                    return 0;

                var alpha = val / distance;
                var beta = UNIT / alpha;

                var div = beta * (val - prev);
                var angle = Math.Round(Math.Acos(distance / Math.Sqrt(div * div + distance * distance)) * 180 / Math.PI);
                if(div < 0)
                    angle = -angle;

                return angle;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }

        public static string RemoveNumber(this string val)
        {
            return val.Replace("1", "")
                .Replace("2", "")
                .Replace("3", "")
                .Replace("4", "")
                .Replace("5", "")
                .Replace("6", "")
                .Replace("7", "")
                .Replace("8", "")
                .Replace("9", "")
                .Replace("0", "");
        }
        //fix Lỗi trong khi hiển thị báo cáo tài chính của một số mã
        public static int AddQuarter(this int val, int num)
        {
            var year = val / 10;
            var quarter = val - year * 10;

            var quarterNext = quarter + num;
            if (quarterNext == 0)
                quarterNext = 4;

            var yearNext = year - quarterNext / 4;
            return int.Parse($"{yearNext}{quarterNext}");
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

        public static long GetPrevQuarter(this int time)
        {
            var year = time / 10;
            var quarter = time - year * 10;
            if (quarter == 1)
            {
                year--;
                quarter = 4;
            }
            else
            {
                quarter--;
            }
            return int.Parse($"{year}{quarter}");
        }

        public static string GetNameMonth(this int time)
        {
            var year = time / 100;
            var month = time - year * 100;
            return $"{month.To2Digit()}/{year - 2000}";
        }

        public static DateTime ToDateTime(this string val, string format)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val))
                    return DateTime.MinValue;
                DateTime dt = DateTime.ParseExact(val, format, CultureInfo.InvariantCulture);
                return dt;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime UnixTimeStampToDateTime(this decimal unixTimeStamp, bool isSecond = true)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            if (isSecond)
            {
                dateTime = dateTime.AddSeconds((double)unixTimeStamp).ToLocalTime();
            }
            else
            {
                dateTime = dateTime.AddMilliseconds((double)unixTimeStamp).ToLocalTime();
            }

            return dateTime;
        }

        public static string To2Digit(this int val)
        {
            if (val > 9)
                return val.ToString();
            return $"0{val}";
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

        public static string RemoveSignVietnamese(this string val)
        {
            var VietnameseSigns = new string[]
            {

                "aAeEoOuUiIdDyY",

                "áàạảãâấầậẩẫăắằặẳẵ",

                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",

                "éèẹẻẽêếềệểễ",

                "ÉÈẸẺẼÊẾỀỆỂỄ",

                "óòọỏõôốồộổỗơớờợởỡ",

                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",

                "úùụủũưứừựửữ",

                "ÚÙỤỦŨƯỨỪỰỬỮ",

                "íìịỉĩ",

                "ÍÌỊỈĨ",

                "đ",

                "Đ",

                "ýỳỵỷỹ",

                "ÝỲỴỶỸ"
            };
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    val = val.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return val;
        }

        public static string ShowLimit(this string val, int num = 10)
        {
            var res = string.Empty;
            if(string.IsNullOrWhiteSpace(val))
            {
                for(int i = 0; i < num; i++)
                {
                    res += "  ";
                }
            }
            else
            {
                var div = num - val.Length;
                if(div < 0)
                {
                    res = val.Substring(0, num);
                }
                else
                {
                    for(int i = 0;i < div;i++)
                    {
                        res +="  ";
                    }
                    res += val;
                }
            }
            return res;
        }

        public static bool IsTonKho(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.XayDung,
                (int)EStockType.KCN,
                (int)EStockType.BDS,
                (int)EStockType.Thep,
                (int)EStockType.BanLe,
                (int)EStockType.Oto,
                (int)EStockType.OtoTai,
                (int)EStockType.PhanBon,
                (int)EStockType.Than,
                (int)EStockType.XiMang,
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsNguoiMua(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.KCN,
                (int)EStockType.BDS
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsXNK(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.Thep,
                (int)EStockType.Than,
                (int)EStockType.ThuySan,
                (int)EStockType.PhanBon,
                (int)EStockType.HoaChat,
                (int)EStockType.Go,
                (int)EStockType.Gao,
                (int)EStockType.XiMang,
                (int)EStockType.CaPhe,
                (int)EStockType.CaoSu,
                (int)EStockType.Oto,
                (int)EStockType.OtoTai,
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsBanLe(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.BanLe
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsHangKhong(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.HangKhong
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsCaoSu(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.CaoSu
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsNganHang(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.NganHang
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsChungKhoan(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.ChungKhoan
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static (bool, List<Quote>) IsWyckoff(this IEnumerable<Quote> lData)
        {
            try
            {
                if ((lData?.Count() ?? 0) < 100)
                    return (false, null);

                var lbb = lData.GetBollingerBands();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);
                var cur = lData.Last();
                var lSOS = lData.SkipLast(3).TakeLast(30);
                var lWyc = new List<Quote>();
                foreach (var itemSOS in lSOS)
                {
                    try
                    {
                        var ma20Vol = lMaVol.First(x => x.Date == itemSOS.Date);
                        if (itemSOS.Volume < 2 * (decimal)ma20Vol.Sma.Value) continue;

                        //Biên độ dao động <= 10% và SOS >= max
                        var lPrev15 = lData.Where(x => x.Date < itemSOS.Date).TakeLast(15);
                        var maxPrev = lPrev15.Max(x => Math.Max(x.Open, x.Close));
                        var minPrev = lPrev15.Min(x => Math.Min(x.Open, x.Close));
                        var rateMaxMin = Math.Round(100 * (-1 + maxPrev / minPrev));
                        if (rateMaxMin > 50
                            || itemSOS.Close < maxPrev) continue;

                        //Nến liền trước
                        var prevSOS = lPrev15.Last();
                        var rate = Math.Round(100 * (-1 + itemSOS.Close / prevSOS.Close));
                        if (rate < 4) continue;

                        #region 10 nến tiếp theo 
                        var lEntry = lData.Where(x => x.Date > itemSOS.Date).Take(10);
                        var countEntry = lEntry.Count();
                        if (countEntry < 4)
                            continue;

                        var countGreater = 0;
                        foreach (var itemEntry in lEntry)
                        {
                            if (itemEntry.Close > itemSOS.Close)
                                countGreater++;
                        }
                        if (countGreater >= 5
                            || countGreater >= countEntry - 1)
                            continue;
                        #endregion

                        #region 20 nến tiếp theo 
                        var lEntryBelow = lData.Where(x => x.Date > itemSOS.Date).Take(20);
                        var countBelow = 0;
                        foreach (var itemEntry in lEntryBelow)
                        {
                            var bb = lbb.First(x => x.Date == itemEntry.Date);
                            if ((decimal)bb.Sma.Value > itemEntry.Close)
                                countBelow++;
                        }
                        if (countBelow >= 5)
                            continue;
                        #endregion

                        //rate Hiện tại
                        var rateCur = Math.Round(100 * (-1 + cur.Close / itemSOS.Close));
                        if(rateCur >= 10) continue;

                        if (lWyc.Any(x => x.Date == itemSOS.Date))
                            continue;

                        lWyc.Add(itemSOS);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                if(lWyc.Any())
                {
                    return (true, lWyc);
                }    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }
    }
}
