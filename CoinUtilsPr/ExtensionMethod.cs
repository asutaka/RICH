using CoinUtilsPr.DAL.Entity;
using CoinUtilsPr.Model;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CoinUtilsPr
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

        public static (bool, DateTime) IsExistTopB(this List<Quote> lData)
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

        public static double GetAngle(this Quote? val, Quote prev, int distance)
        {
            try
            {
                if (val is null || distance < 5)
                    return 0;

                var div = Math.Abs(val.Close - prev.Close);

                return Math.Acos(distance / Math.Sqrt((double)(div * div + distance * distance)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }

        public static List<TopBotModel> GetTopBottom_H(this List<Quote> lData, int minrate)
        {
            var lResult = new List<TopBotModel>();
            try
            {
                if (lData is null)
                    return lResult;

                var count = lData.Count;
                if (count < 5)
                    return lResult;

                for (var i = 6; i <= count - 1; i++)
                {
                    var itemNext1 = lData.ElementAt(i);
                    var itemCheck = lData.ElementAt(i - 1);
                    var itemPrev1 = lData.ElementAt(i - 2);
                    var itemPrev2 = lData.ElementAt(i - 3);
                    var itemPrev3 = lData.ElementAt(i - 4);

                    var last = lResult.LastOrDefault();
                    if (itemCheck.Low < Math.Min(Math.Min(itemPrev1.Low, itemPrev2.Low), itemPrev3.Low)
                        && itemCheck.Low < itemNext1.Low)
                    {
                        var model = new TopBotModel { Date = itemCheck.Date, IsTop = false, IsBot = true, Value = itemCheck.Low, Item = itemCheck };
                        if (last != null)
                        {
                            if (last.IsBot)
                            {
                                if (last.Value > model.Value)
                                {
                                    lResult.Remove(last);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                var rate = Math.Abs(Math.Round(100 * (-1 + last.Value / model.Value)));
                                if (rate < minrate)
                                {
                                    continue;
                                }
                            }
                        }
                        lResult.Add(model);
                    }
                    else if (itemCheck.High > Math.Max(Math.Max(itemPrev1.High, itemPrev2.High), itemPrev3.High)
                        && itemCheck.High > itemNext1.High)
                    {
                        var model = new TopBotModel { Date = itemCheck.Date, IsTop = true, IsBot = false, Value = itemCheck.High, Item = itemCheck };
                        if (last != null)
                        {
                            if (last.IsTop)
                            {
                                if (last.Value < model.Value)
                                {
                                    lResult.Remove(last);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                var rate = Math.Abs(Math.Round(100 * (-1 + model.Value / last.Value)));
                                if (rate < minrate)
                                {
                                    continue;
                                }
                            }
                        }
                        lResult.Add(model);
                    }
                }
                return lResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExtensionMethod.GetTopBottom_HL|EXCEPTION| {ex.Message}");
            }
            return lResult;
        }

        public static (bool, QuoteEx) IsFlagBuy(this IEnumerable<Quote> lData)
        {
            decimal BB_Min = 1m;
            decimal RateTP_Min = 2.5m;
            decimal RateTP_Max = 7m;
            try
            {
                if ((lData?.Count() ?? 0) < 50)
                    return (false, null);

                lData = lData.TakeLast(80).ToList();

                var lbb = lData.GetBollingerBands();
                var lrsi = lData.GetRsi();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);

                var e_Cur = lData.Last();
                var bb_Cur = lbb.First(x => x.Date == e_Cur.Date);

                var e_Pivot = lData.SkipLast(1).Last();
                var rsi_Pivot = lrsi.First(x => x.Date == e_Pivot.Date);
                var bb_Pivot = lbb.First(x => x.Date == e_Pivot.Date);
                var vol_Pivot = lMaVol.First(x => x.Date == e_Pivot.Date);

                var e_Sig = lData.SkipLast(2).Last();
                var rsi_Sig = lrsi.First(x => x.Date == e_Sig.Date);
                var bb_Sig = lbb.First(x => x.Date == e_Sig.Date);
                var vol_Sig = lMaVol.First(x => x.Date == e_Sig.Date);

                //Vol pivot phải gấp đôi 2 vol liền trước và liền sau
                if(e_Sig.Volume == 0 
                    || e_Cur.Volume == 0
                    || e_Pivot.Volume == 0)
                {
                    return (false, null);
                }
                var rateVolSig = Math.Round(e_Pivot.Volume / e_Sig.Volume, 1);
                var rateVolCur = Math.Round(e_Pivot.Volume / e_Cur.Volume, 1);   

                var flag = true
                        && e_Pivot.Open > e_Pivot.Close
                        && e_Pivot.Low < (decimal)bb_Pivot.LowerBand.Value
                        && e_Pivot.High < (decimal)bb_Pivot.Sma.Value
                        && e_Pivot.Volume >= 1.5m * (decimal)vol_Pivot.Sma.Value
                        && rateVolSig >= 2
                        && rateVolCur >= 2
                        && true;

                if (!flag)
                    return (false, null);

                //Đếm số nến
                var NUM_CHECK = 20;
                var lCheck = lData.Where(x => x.Date < e_Pivot.Date).TakeLast(NUM_CHECK);
                var count_UPMa20 = 0;
                var count_CUPMa20 = 0;
                var count_GREEN = 0;
                var lavg = new List<decimal>();
                var index = 0;
                double bb_Prev20 = 0;

                foreach (var itemzz in lCheck)
                {
                    var bb = lbb.First(x => x.Date == itemzz.Date);
                    if (index == 0)
                    {
                        bb_Prev20 = bb.UpperBand.Value - bb.LowerBand.Value;
                    }

                    if (itemzz.High > (decimal)bb.Sma.Value)
                        count_UPMa20++;

                    if (itemzz.Close > (decimal)bb.Sma.Value)
                        count_CUPMa20++;

                    if (itemzz.Close > itemzz.Open)
                        count_GREEN++;

                    var len = Math.Round(100 * (-1 + itemzz.High / itemzz.Low), 2);
                    lavg.Add(len);
                    index++;
                }
                var avg = lavg.Average();

                var lenPivot = Math.Round(100 * (-1 + e_Pivot.High / e_Pivot.Low), 2);
                var lenPivotRate = Math.Round(lenPivot / avg, 1);
                if (lenPivotRate > 3m)
                    return (false, null);

                var lenCur = Math.Round(100 * (-1 + e_Cur.High / e_Cur.Low), 2);
                var lenCurRate = Math.Round(lenCur / avg, 1);
                if (lenCurRate > 1.5m)
                    return (false, null);

                var lenPrev5 = Math.Round(lCheck.TakeLast(5).Max(x => Math.Round(100 * (-1 + x.High / x.Low), 2)) / avg, 1);
                if (lenPrev5 > 3.5m)
                    return (false, null);

                var bbPivot = lbb.First(x => x.Date == e_Pivot.Date);
                var bbRate20 = Math.Round((bbPivot.UpperBand.Value - bbPivot.LowerBand.Value) / bb_Prev20, 1);
                if (bbRate20 > 4)
                    return (false, null);

                var rateUPMa20 = Math.Round(100 * (decimal)count_UPMa20 / NUM_CHECK, 1);
                var rateCUPMa20 = Math.Round(100 * (decimal)count_CUPMa20 / NUM_CHECK, 1);
                var rateGREEN = Math.Round(100 * (decimal)count_GREEN / NUM_CHECK, 1);
                if (false) { }
                else if (rateUPMa20 <= 20)
                {
                    return (false, null);
                }
                else if (rateUPMa20 == 100)
                {
                    if (rateCUPMa20 >= 85)
                        return (false, null);
                }
                else if (rateGREEN < 30)
                {
                    return (false, null);
                }


                var rateBB = (Math.Round(100 * (-1 + (decimal)bb_Pivot.UpperBand.Value / e_Cur.Close)) - 1);
                if (rateBB < BB_Min)
                {
                    return (false, null);
                }

                if(e_Cur.Close > Math.Max(e_Pivot.Open, e_Pivot.Close))
                {
                    return (false, null);
                }

                var rate_TP = (decimal)(Math.Round(100 * (-1 + bb_Cur.UpperBand.Value / bb_Cur.LowerBand.Value)) - 1);
                if (rate_TP > RateTP_Max)
                {
                    rate_TP = RateTP_Max;
                }
                else if (rate_TP < RateTP_Min)
                {
                    rate_TP = RateTP_Min;
                }

                return (true, new QuoteEx
                {
                    Date = e_Cur.Date,
                    Open = e_Cur.Open,
                    Close = e_Cur.Close,
                    High = e_Cur.High,
                    Low = e_Cur.Low,
                    Volume = e_Cur.Volume,
                    Rate_TP = rate_TP,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, QuoteEx) IsFlagBuy_Doji(this IEnumerable<Quote> lData)
        {
            decimal BB_Min = 1m;
            decimal RateTP_Min = 2.5m;
            decimal RateTP_Max = 7m;
            try
            {
                if ((lData?.Count() ?? 0) < 50)
                    return (false, null);

                lData = lData.TakeLast(80).ToList();
                var lbb = lData.GetBollingerBands();

                var e_Cur = lData.Last();
                var bb_Cur = lbb.First(x => x.Date == e_Cur.Date);

                var e_Pivot = lData.SkipLast(1).Last();
                var e_Sig = lData.SkipLast(2).Last();
                if (e_Sig.Volume == 0
                    || e_Cur.Volume == 0
                    || e_Pivot.Volume == 0)
                {
                    return (false, null);
                }

                if (e_Cur.Open > (decimal)bb_Cur.LowerBand.Value)
                {
                    return (false, null);
                }

                var thannen = e_Cur.Open - e_Cur.Close;
                var dodainen = e_Cur.High - e_Cur.Low;
                var rauduoi = Math.Min(e_Cur.Open, e_Cur.Close) - e_Cur.Low;
                var rautren = e_Cur.High - Math.Max(e_Cur.Open, e_Cur.Close);

                var rate_thannen = thannen / dodainen;
                if (rate_thannen >= 0.5m)
                    return (false, null);

                var rate_rauduoi = rauduoi / dodainen;
                if (rate_rauduoi < 0.2m)
                    return (false, null);

                var rate_rautren = rautren / dodainen;
                if (rate_rautren < 0.2m)
                    return (false, null);

                var countViolate = lData.SkipLast(1).TakeLast(5).Count(x => x.Low < e_Cur.Open);
                if (countViolate >= 2)
                    return (false, null);

                var rate_TP = (decimal)(Math.Round(100 * (-1 + bb_Cur.UpperBand.Value / bb_Cur.LowerBand.Value)) - 1);
                if (rate_TP > RateTP_Max)
                {
                    rate_TP = RateTP_Max;
                }
                else if (rate_TP < RateTP_Min)
                {
                    rate_TP = RateTP_Min;
                }

                return (true, new QuoteEx
                {
                    Date = e_Cur.Date,
                    Open = e_Cur.Open,
                    Close = e_Cur.Close,
                    High = e_Cur.High,
                    Low = e_Cur.Low,
                    Volume = e_Cur.Volume,
                    Rate_TP = rate_TP,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, QuoteEx) IsFlagSell(this IEnumerable<Quote> lData)
        {
            decimal BB_Min = 1m;
            decimal RateTP_Min = 2.5m;
            decimal RateTP_Max = 7m;
            try
            {
                if ((lData?.Count() ?? 0) < 50)
                    return (false, null);

                lData = lData.TakeLast(80).ToList();
                var lbb = lData.GetBollingerBands();
                var lrsi = lData.GetRsi();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);

                var e_Cur = lData.Last();
                var bb_Cur = lbb.First(x => x.Date == e_Cur.Date);

                var e_Pivot = lData.SkipLast(1).Last();
                var rsi_Pivot = lrsi.First(x => x.Date == e_Pivot.Date);
                var bb_Pivot = lbb.First(x => x.Date == e_Pivot.Date);
                var vol_Pivot = lMaVol.First(x => x.Date == e_Pivot.Date);

                var e_Sig = lData.SkipLast(2).Last();
                var rsi_Sig = lrsi.First(x => x.Date == e_Sig.Date);
                var bb_Sig = lbb.First(x => x.Date == e_Sig.Date);
                var vol_Sig = lMaVol.First(x => x.Date == e_Sig.Date);

                //Vol pivot phải gấp đôi 2 vol liền trước và liền sau
                if (e_Sig.Volume == 0
                    || e_Cur.Volume == 0
                    || e_Pivot.Volume == 0)
                {
                    return (false, null);
                }
                var rateVolSig = Math.Round(e_Pivot.Volume / e_Sig.Volume, 1);
                var rateVolCur = Math.Round(e_Pivot.Volume / e_Cur.Volume, 1);

                var flag = true
                        && e_Pivot.Open < e_Pivot.Close
                        && e_Pivot.High > (decimal)bb_Pivot.UpperBand.Value
                        && e_Pivot.Low > (decimal)bb_Pivot.Sma.Value
                        && e_Pivot.Volume >= 1.5m * (decimal)vol_Pivot.Sma.Value
                        && rateVolSig >= 2
                        && rateVolCur >= 2
                        && true;

                if (!flag)
                    return (false, null);

                //Độ dài nến hiện tại
                var rateCur = 100 * (-1 + e_Cur.High / e_Cur.Low);
                if(rateCur > 4 && e_Cur.Open > e_Cur.Close)
                    return (false, null);

                //Đếm số nến
                var NUM_CHECK = 20;
                var lCheck = lData.Where(x => x.Date < e_Pivot.Date).TakeLast(NUM_CHECK);
                var count_UPMa20 = 0;
                var count_CUPMa20 = 0;
                var count_RED = 0;
                var lavg = new List<decimal>();
                var index = 0;
                double bb_Prev20 = 0;

                foreach (var itemzz in lCheck)
                {
                    var bb = lbb.First(x => x.Date == itemzz.Date);
                    if (index == 0)
                    {
                        bb_Prev20 = bb.UpperBand.Value - bb.LowerBand.Value;
                    }

                    if (itemzz.Low < (decimal)bb.Sma.Value)
                        count_UPMa20++;

                    if (itemzz.Close < (decimal)bb.Sma.Value)
                        count_CUPMa20++;

                    if (itemzz.Close < itemzz.Open)
                        count_RED++;

                    var len = Math.Round(100 * (-1 + itemzz.High / itemzz.Low), 2);
                    lavg.Add(len);
                    index++;
                }
                var avg = lavg.Average();

                var lenPivot = Math.Round(100 * (-1 + e_Pivot.High / e_Pivot.Low), 2);
                var lenPivotRate = Math.Round(lenPivot / avg, 1);
                if (lenPivotRate > 3m)
                    return (false, null);

                var lenCur = Math.Round(100 * (-1 + e_Cur.High / e_Cur.Low), 2);
                var lenCurRate = Math.Round(lenCur / avg, 1);
                if (lenCurRate > 1.5m)
                    return (false, null);

                var lenPrev5 = Math.Round(lCheck.TakeLast(5).Max(x => Math.Round(100 * (-1 + x.High / x.Low), 2)) / avg, 1);
                if (lenPrev5 > 3.5m)
                    return (false, null);

                var bbPivot = lbb.First(x => x.Date == e_Pivot.Date);
                var bbRate20 = Math.Round((bbPivot.UpperBand.Value - bbPivot.LowerBand.Value) / bb_Prev20, 1);
                if (bbRate20 > 4)
                    return (false, null);

                var rateUPMa20 = Math.Round(100 * (decimal)count_UPMa20 / NUM_CHECK, 1);
                var rateCUPMa20 = Math.Round(100 * (decimal)count_CUPMa20 / NUM_CHECK, 1);
                var rateRED = Math.Round(100 * (decimal)count_RED / NUM_CHECK, 1);
                if (false) { }
                else if (rateUPMa20 <= 20)
                {
                    return (false, null);
                }
                else if (rateUPMa20 == 100)
                {
                    if (rateCUPMa20 >= 85)
                        return (false, null);
                }
                else if (rateRED < 30)
                {
                    return (false, null);
                }


                var rateBB = (Math.Round(100 * (-1 + e_Cur.Close / (decimal)bb_Pivot.LowerBand.Value)) - 1);
                if (rateBB < BB_Min)
                {
                    return (false, null);
                }

                if (e_Cur.Close < Math.Min(e_Pivot.Open, e_Pivot.Close))
                {
                    return (false, null);
                }

                var rate_TP = (decimal)(Math.Round(100 * (-1 + bb_Cur.UpperBand.Value / bb_Cur.LowerBand.Value)) - 1);
                if (rate_TP > RateTP_Max)
                {
                    rate_TP = RateTP_Max;
                }
                else if (rate_TP < RateTP_Min)
                {
                    rate_TP = RateTP_Min;
                }

                return (true, new QuoteEx
                {
                    Date = e_Cur.Date,
                    Open = e_Cur.Open,
                    Close = e_Cur.Close,
                    High = e_Cur.High,
                    Low = e_Cur.Low,
                    Volume = e_Cur.Volume,
                    Rate_TP = rate_TP,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, QuoteEx) IsFlagSell_Doji(this IEnumerable<Quote> lData)
        {
            decimal BB_Min = 1m;
            decimal RateTP_Min = 2.5m;
            decimal RateTP_Max = 7m;
            try
            {
                if ((lData?.Count() ?? 0) < 50)
                    return (false, null);

                lData = lData.TakeLast(80).ToList();
                var lbb = lData.GetBollingerBands();

                var e_Cur = lData.Last();
                var bb_Cur = lbb.First(x => x.Date == e_Cur.Date);

                var e_Pivot = lData.SkipLast(1).Last();
                var e_Sig = lData.SkipLast(2).Last();
                if (e_Sig.Volume == 0
                    || e_Cur.Volume == 0
                    || e_Pivot.Volume == 0)
                {
                    return (false, null);
                }

                if(e_Cur.Open < (decimal)bb_Cur.UpperBand.Value)
                {
                    return (false, null);
                }

                var thannen = e_Cur.Open - e_Cur.Close;
                var dodainen = e_Cur.High - e_Cur.Low;
                var rauduoi = Math.Min(e_Cur.Open, e_Cur.Close) - e_Cur.Low;
                var rautren = e_Cur.High - Math.Max(e_Cur.Open, e_Cur.Close);

                var rate_thannen = thannen / dodainen;
                if (rate_thannen >= 0.5m)
                    return (false, null);

                var rate_rauduoi = rauduoi / dodainen;
                if (rate_rauduoi < 0.2m)
                    return (false, null);

                var rate_rautren = rautren / dodainen;
                if (rate_rautren < 0.2m)
                    return (false, null);

                var countViolate = lData.SkipLast(1).TakeLast(5).Count(x => x.High > e_Cur.Open);
                if (countViolate >= 2)
                    return (false, null);

                var rate_TP = (decimal)(Math.Round(100 * (-1 + bb_Cur.UpperBand.Value / bb_Cur.LowerBand.Value)) - 1);
                if (rate_TP > RateTP_Max)
                {
                    rate_TP = RateTP_Max;
                }
                else if (rate_TP < RateTP_Min)
                {
                    rate_TP = RateTP_Min;
                }

                return (true, new QuoteEx {
                                            Date = e_Cur.Date,
                                            Open = e_Cur.Open,
                                            Close = e_Cur.Close,
                                            High = e_Cur.High,
                                            Low = e_Cur.Low,
                                            Volume = e_Cur.Volume,
                                            Rate_TP = rate_TP,
                                        });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, Quote) IsWyckoff(this IEnumerable<Quote> lData, decimal biendonenphang = 3, decimal nenlientruoc = 1)
        {
            try
            {
                if ((lData?.Count() ?? 0) < 100)
                    return (false, null);

                var lbb = lData.GetBollingerBands();
                var lrsi = lData.GetRsi();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);
                var lTake = lData.SkipLast(3).TakeLast(35);
                var lSOS = lData.TakeLast(15); //4 - 15
                var lWyc = new List<Quote>();
                foreach (var itemSOS in lSOS.OrderByDescending(x => x.Date))
                {
                    try
                    {
                        if (itemSOS.Open > itemSOS.Close) continue; //Loại nếu nến đỏ
                        var rsi = lrsi.First(x => x.Date == itemSOS.Date);
                        if (rsi.Rsi <= 50) continue;//RSI phải lớn hơn 50
                        var ma20Vol = lMaVol.First(x => x.Date == itemSOS.Date);
                        if (itemSOS.Volume <= (decimal)ma20Vol.Sma.Value) continue; //Vol phải lớn hơn TB 20 phiên

                        var countMaxVolPrevSOS = lTake.Where(x => x.Date < itemSOS.Date).TakeLast(20).Count(x => x.Volume > itemSOS.Volume);
                        if(countMaxVolPrevSOS > 1) continue; //Vol phải lớn hơn 20 nến liền trước

                        var maxClosePrevSOS = lTake.Where(x => x.Date < itemSOS.Date).TakeLast(20).Max(x => x.Close);
                        if(itemSOS.Close < maxClosePrevSOS) continue; //Close phải lớn hơn 20 nến liền trước

                        var bb = lbb.First(x => x.Date == itemSOS.Date);
                        var bb_Prev_10 = lbb.Where(x => x.Date < itemSOS.Date).SkipLast(10).Last();
                        var bb_Prev_30 = lbb.Where(x => x.Date < itemSOS.Date).SkipLast(50).Last();

                        var divBB = bb.UpperBand - bb.LowerBand;
                        var divBB_Prev_10 = bb_Prev_10.UpperBand - bb_Prev_10.LowerBand;
                        var divBB_Prev_30 = bb_Prev_30.UpperBand - bb_Prev_30.LowerBand;
                        if(divBB_Prev_10 > 1.5 * divBB_Prev_30) continue;//Nếu band quá khứ 10 lớn hơn 1.5 lần band quá khứ 30 -> loại(chỉ lấy tại lần thứ nhất)
                        if(divBB > 2  * divBB_Prev_10) continue;//Nếu band hiện tại gấp đôi band quá khứ 10 -> loại

                        if(itemSOS.Date.Day == 9 && itemSOS.Date.Month == 7 && itemSOS.Date.Hour == 16)
                        {
                            var tmp = 1;
                        }

                        var lNext = lData.Where(x => x.Date > itemSOS.Date).Skip(2).Take(13);
                        foreach (var itemNext in lNext)
                        {
                            var rsiNext = lrsi.First(x => x.Date == itemNext.Date);
                            if (rsiNext.Rsi < 50) //RSI Entry không được nhỏ hơn 50
                                return (false, null);

                            if (rsiNext.Rsi > 60) continue;//Entry chỉ khi RSI <= 70(CK: 70, coin: 60)

                            var bb_Next = lbb.First(x => x.Date == itemNext.Date);
                            var div_Upper_Low = (decimal)bb_Next.UpperBand - itemNext.Low;
                            var div_Low_Ma20 = itemNext.Low - (decimal)bb_Next.Sma.Value;
                            if (div_Upper_Low < 3 * div_Low_Ma20) continue;//Low cuả Next <= 1/4 của Ma20 -> Upper(sự khác biệt giữa coin và chứng khoán)

                            var maxClose = lData.Where(x => x.Date >= itemSOS.Date && x.Date <= itemNext.Date).MaxBy(x => x.Close);
                            var divSOS = maxClose.Date - itemSOS.Date;
                            var divNext = itemNext.Date - maxClose.Date;
                            if (divNext < divSOS) break;//Số nến phân phối phải lớn hơn số nến SOS



                            Console.WriteLine($"SOS: {itemSOS.Date.ToString("dd/MM/yyyy HH")}| Entry: {itemNext.Date.ToString("dd/MM/yyyy HH")}");
                            break;
                        }

                        //Console.WriteLine($"SOS: {itemSOS.Date.ToString("dd/MM/yyyy HH")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }
    
        public static bool IsWyckoffOut(this Quote val, IEnumerable<Quote> lData)
        {
            try
            {
                var lRsi = lData.GetRsi();
                var lBB = lData.GetBollingerBands();
                var rsi = lRsi.Last();
                if (rsi.Rsi >= 75)
                    return true;

                var rsi_Prev = lRsi.SkipLast(1).Last();
                if(rsi_Prev.Rsi > 70)
                    return true;

                var cur = lData.SkipLast(1).Last();
                var prev = lData.SkipLast(2).Last();
                var bb_Prev = lBB.First(x => x.Date == prev.Date);

                if (prev.Close > (decimal)bb_Prev.UpperBand.Value
                    && prev.Volume >= 2 * cur.Volume)
                    return true;

                var count = lData.Count(x => x.Date > val.Date);
                if(count > 24)
                    return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }
    }
}
