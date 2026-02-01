using Skender.Stock.Indicators;
using StockPr.DAL.Entity;
using StockPr.Model;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace StockPr.Utils
{
    public static class ExtensionMethod
    {
        public static string RemoveSpace(this string val)
        {
            return val.Replace(" ", "").Replace(",", "").Replace(".", "").Replace("-", "").Replace("_", "").Replace("(", "").Replace(")", "").Trim();
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
                if (div < 0)
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
            if (string.IsNullOrWhiteSpace(val))
            {
                for (int i = 0; i < num; i++)
                {
                    res += "  ";
                }
            }
            else
            {
                var div = num - val.Length;
                if (div < 0)
                {
                    res = val.Substring(0, num);
                }
                else
                {
                    for (int i = 0; i < div; i++)
                    {
                        res += "  ";
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
                        if (rateCur >= 10) continue;

                        if (lWyc.Any(x => x.Date == itemSOS.Date))
                            continue;

                        lWyc.Add(itemSOS);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                if (lWyc.Any())
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lData"></param>
        /// <param name="lInfo"></param>
        /// <param name="preEntry"></param>
        /// <returns>
        /// 0: không làm gì 
        /// -1: xóa preEntry
        /// -2: update preEntry Pressure
        /// -3: update preEntry NN1
        /// 1: Pressure
        /// 2: RSI 
        /// 4: NN1
        /// 8: NN2
        /// 16: NN3
        /// 32: Wyckoff 
        /// </returns>
        public static EntryModel CheckEntry(this List<QuoteT> lData, SSI_DataStockInfoResponse lInfo, PreEntry preEntry)
        {
            var output = new EntryModel
            {
                Response = EEntry.NONE,
            };
            try
            {
                if ((lData?.Count() ?? 0) < 100)
                    return output;

                if (preEntry is null)
                {
                    preEntry = new PreEntry();
                }

                if (lInfo is null) 
                {
                    lInfo = new SSI_DataStockInfoResponse
                    {
                        data = new List<SSI_DataStockInfoDetailResponse>()
                    };
                }    

                var cur = lData[^1];
                // Filter 1: Minimum Price (≥ 5,000 VND)
                if (cur.Close < 5) return output;
                // Filter 2: Minimum Average Volume (≥ 100,000 shares/day)
                var recentVolumes = lData.TakeLast(20).Select(x => x.Volume).ToList();
                var avgVolume = recentVolumes.Average();
                if (avgVolume < 100000) return output;
                // Filter 3: Minimum Liquidity (Price × Volume ≥ 500M VND)
                var liquidity = cur.Close * avgVolume;
                if (liquidity < 500000) return output;
                // Tính BB
                var lbb = lData.GetBollingerBands(20, 2).ToList();
                var lrsi = lData.GetRsi(14).ToList();
                var lma9 = lrsi.GetSma(9).ToList();
                foreach (var item in lInfo.data)
                {
                    var date = DateTime.ParseExact(item.tradingDate, "dd/MM/yyyy", null);
                    item.TimeStamp = new DateTimeOffset(date.Date, TimeSpan.Zero).ToUnixTimeSeconds();
                }
                // Tìm entries
                var bb_cur = lbb[^1];

                var prev_1 = lData[^2];
                var bb_prev_1 = lbb[^2];

                var prev_2 = lData[^3];
                var bb_prev_2 = lbb[^3];

                var sma = (decimal)bb_cur.Sma.Value;
                var lower = (decimal)bb_cur.LowerBand.Value;
                var upper = (decimal)bb_cur.UpperBand.Value;

                // Check BB Entry (1/2 zones)
                var midLower = sma - (sma - lower) / 2;
                var midUpper = sma + (upper - sma) / 2;

                var midLower_14 = lower + (sma - lower) / 4;
                var midUpper_14 = sma + (upper - sma) / 4;

                var maxPrev = Math.Max(Math.Max(prev_1.Open, prev_1.Close), prev_2.Close);
                var minPrev = Math.Min(Math.Min(prev_1.Open, prev_1.Close), prev_2.Close);
                var maxCur = Math.Max(cur.Open, cur.Close);
                var minCur = Math.Min(cur.Open, cur.Close);

                bool isEntryDown = (maxCur <= midLower && maxPrev < sma);
                bool isEntryUp = (minCur >= sma && maxCur <= midUpper && minPrev >= sma);

                bool isBBEntry = isEntryDown || isEntryUp;

                if (!isBBEntry)
                {
                    output.preAction = preEntry.d > 0 ? EPreEntryAction.DELETE : EPreEntryAction.NONE;
                    return output;
                }

                var rsi_cur = lrsi[^1];
                var ma9_cur = lma9[^1];
                var rsi_prev_1 = lrsi[^2];
                var ma9_prev_1 = lma9[^2];

                bool isRsi = false, isPressure = false, isNN1 = false, isNN2 = false, isNN3 = false;
                //Tín hiệu RSI  
                if (isEntryDown && rsi_cur.Rsi.Value >= ma9_cur.Sma.Value
                    && rsi_prev_1.Rsi.Value < ma9_prev_1.Sma.Value)
                {
                    isRsi = true;
                    output.Response |= EEntry.RSI;
                }

                //Pressure
                var info_cur = lInfo.data.FirstOrDefault(x => x.TimeStamp == cur.TimeStamp);
                var info_prev_1 = lInfo.data.FirstOrDefault(x => x.TimeStamp == prev_1.TimeStamp);
                var info_prev_2 = lInfo.data.FirstOrDefault(x => x.TimeStamp == prev_2.TimeStamp);
                if (info_cur != null && info_cur.netTotalTradeVol > 0)
                {
                    if (((info_prev_1 is null || info_prev_1.netTotalTradeVol < 0)
                        && (info_prev_2 is null || info_prev_2.netTotalTradeVol < 0))
                        || preEntry.isPrePressure)
                    {
                        // Check BB Entry (1/4 zones)
                        if ((isEntryDown && minCur < midLower_14)
                            || (isEntryUp && minCur < midUpper_14))
                        {
                            isPressure = true;
                            output.Response |= EEntry.PRESSURE;
                            output.preAction = preEntry.d <= 0 ? EPreEntryAction.DELETE : EPreEntryAction.NONE;
                            preEntry.isPrePressure = false;
                        }
                        else
                        {
                            output.preAction = EPreEntryAction.UPDATE;
                            preEntry.isPrePressure = true;
                        }
                    }
                }

                if (info_cur is null || info_cur.netTotalTradeVol <= 0)
                {
                    output.preAction = EPreEntryAction.UPDATE;
                    preEntry.isPrePressure = false;
                }

                //NN
                if (info_cur != null && info_prev_1 != null)
                {
                    if ((info_cur.netBuySellVol > 0 && info_prev_1.netBuySellVol < 0)
                         || preEntry.isPreNN1)
                    {
                        if ((isEntryDown && minCur < midLower_14)
                           || (isEntryUp && minCur < midUpper_14))
                        {
                            isNN1 = true;
                            output.Response |= EEntry.NN1;
                            output.preAction = preEntry.d <= 0 ? EPreEntryAction.DELETE : EPreEntryAction.NONE;
                            preEntry.isPreNN1 = false;
                        }
                        else
                        {
                            output.preAction = EPreEntryAction.UPDATE;
                            preEntry.isPreNN1 = true;
                        }
                    }
                    if (info_cur.netBuySellVol > 0 && info_prev_1.netBuySellVol > 0 && info_cur.netBuySellVol / info_prev_1.netBuySellVol > 2)
                    {
                        output.Response |= EEntry.NN2;
                        isNN2 = true;
                    }
                    if (info_cur.netBuySellVol < 0 && info_prev_1.netBuySellVol < 0 && info_prev_1.netBuySellVol / info_cur.netBuySellVol > 2)
                    {
                        output.Response |= EEntry.NN3;
                        isNN3 = true;
                    }
                    //
                    if (info_cur.netBuySellVol <= 0)
                    {
                        output.preAction = EPreEntryAction.UPDATE;
                        preEntry.isPreNN1 = false;
                    }
                }

                if (isRsi || isPressure || isNN1 || isNN2 || isNN3)
                {
                    output.quote = cur;
                    var checkWyckoff = lData.IsWyckoff();
                    if (checkWyckoff.Item1)
                    {
                        output.Response |= EEntry.WYCKOFF;
                    }
                }

                if (output.preAction == EPreEntryAction.UPDATE) 
                {
                    if(preEntry.d <= 0)
                    {
                        preEntry.d = (int)cur.TimeStamp;
                    }
                    output.pre = preEntry;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }
    }
}
