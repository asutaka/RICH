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
                var UNIT = 120;
                if (distance < 1)
                    return 0;

                var alpha = (double)val.Close / distance;
                var beta = UNIT / alpha;

                var div = beta * (double)(val.Open - prev.Close);
                var angle = Math.Round(Math.Acos(distance / Math.Sqrt(div * div + distance * distance)) * 180 / Math.PI);

                return Math.Abs(angle);
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

        public static (bool, Quote, Quote, double?) IsWyckoff(this IEnumerable<Quote> lData)
        {
            try
            {
                if ((lData?.Count() ?? 0) < 100)
                    return (false, null, null, null);

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
                        var lenSOS = itemSOS.Close - itemSOS.Open;
                        if (lenSOS <= 0) continue; //Loại nếu nến đỏ
                        var rsi = lrsi.First(x => x.Date == itemSOS.Date);
                        if (rsi.Rsi <= 50) continue;//RSI phải lớn hơn 50
                        var ma20Vol = lMaVol.First(x => x.Date == itemSOS.Date);
                        if (itemSOS.Volume <= (decimal)ma20Vol.Sma.Value) continue; //Vol phải lớn hơn TB 20 phiên

                        var countMaxVolPrevSOS = lTake.Where(x => x.Date < itemSOS.Date).TakeLast(20).Count(x => x.Volume > itemSOS.Volume);
                        if(countMaxVolPrevSOS > 1) continue; //Vol phải lớn hơn 20 nến liền trước

                        var maxClosePrevSOS = lTake.Where(x => x.Date < itemSOS.Date).TakeLast(20).Max(x => x.Close);
                        var minClosePrevSOS = lTake.Where(x => x.Date < itemSOS.Date).TakeLast(20).Min(x => x.Close);
                        var maxmin20Prev = maxClosePrevSOS - minClosePrevSOS;
                        if (itemSOS.Close < maxClosePrevSOS) continue; //Close phải lớn hơn 20 nến liền trước

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
                                return (false, null, null, null); 

                            if (rsiNext.Rsi > 60) continue;//Entry chỉ khi RSI <= 70(CK: 70, coin: 60)

                            var bb_Next = lbb.First(x => x.Date == itemNext.Date);
                            var div_Upper_Low = (decimal)bb_Next.UpperBand - itemNext.Low;
                            var div_Low_Ma20 = itemNext.Low - (decimal)bb_Next.Sma.Value;
                            if (div_Upper_Low < 3 * div_Low_Ma20) continue;//Low cuả Next <= 1/4 của Ma20 -> Upper(sự khác biệt giữa coin và chứng khoán)

                            var maxClose = lData.Where(x => x.Date >= itemSOS.Date && x.Date <= itemNext.Date).MaxBy(x => x.Close);
                            var divSOS = maxClose.Date - itemSOS.Date;
                            var divNext = itemNext.Date - maxClose.Date;
                            if (divNext < divSOS) break;//Số nến phân phối phải lớn hơn số nến SOS

                            var num = lData.Count(x => x.Date < itemNext.Date && x.Date >= itemSOS.Date);
                            var angle = itemNext.GetAngle(maxClose, num);

                            if (lenSOS > maxmin20Prev && angle > 30) break;//Loại Nếu độ dài SOS quá lớn và phân phối quá gắt 

                            //Console.WriteLine($"SOS: {itemSOS.Date.ToString("dd/MM/yyyy HH")}| Entry: {itemNext.Date.ToString("dd/MM/yyyy HH")}");
                            return (true, itemNext, itemSOS, angle);
                        }
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

            return (false, null, null, null);
        }

        public static (bool, Quote, Quote, double?) IsWyckoff_20250726(this IEnumerable<Quote> lData)
        {
            try
            {
                if ((lData?.Count() ?? 0) < 150)
                    return (false, null, null, null);

                var lbb = lData.GetBollingerBands();
                var lrsi = lData.GetRsi();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);
                var lSOS = lData.TakeLast(72); 
                var lWyc = new List<Quote>();
                foreach (var itemSOS in lSOS.OrderByDescending(x => x.Date))
                {
                    try
                    {
                        var lenSOS = itemSOS.Close - itemSOS.Open;
                        if (lenSOS <= 0) continue; //Loại nếu nến đỏ
                        var rsi = lrsi.First(x => x.Date == itemSOS.Date);
                        if (rsi.Rsi <= 50) continue;//RSI phải lớn hơn 50
                        var ma20Vol = lMaVol.First(x => x.Date == itemSOS.Date);
                        if (itemSOS.Volume <= 2m * (decimal)ma20Vol.Sma.Value) continue; //Vol phải lớn hơn MA20

                        var countMaxVolPrevSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(35).Count(x => x.Volume > itemSOS.Volume);
                        if (countMaxVolPrevSOS >= 1) continue; //Vol phải lớn hơn 35 nến liền trước

                        var maxClosePrevSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(35).Max(x => x.Close);
                        var minClosePrevSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(35).Min(x => x.Close);
                        var maxmin20Prev = maxClosePrevSOS - minClosePrevSOS;
                        if (itemSOS.Close < maxClosePrevSOS) continue; //Close phải lớn hơn 35 nến liền trước

                        if ((itemSOS.Close - itemSOS.Open) > 2.5m * maxmin20Prev) continue;//Độ dài SOS không được lớn hơn 2.5 lần độ rộng của 35 nến trước đó

                        var lBB_Prev100 = lbb.Where(x => x.Date < itemSOS.Date).TakeLast(100);
                        var maxBB = lBB_Prev100.Max(x => x.UpperBand - x.LowerBand);
                        var minBB = lBB_Prev100.Min(x => x.UpperBand - x.LowerBand);
                        var avgBB = 0.5 * (maxBB + minBB);

                        var bbPrev1 = lbb.Last(x => x.Date < itemSOS.Date);
                        var bbPrev1_Val = bbPrev1.UpperBand - bbPrev1.LowerBand;
                        var ratePrev1 = Math.Round(bbPrev1_Val.Value / avgBB.Value, 2);

                        var bbPrev10 = lbb.Where(x => x.Date < itemSOS.Date).SkipLast(9).Last();
                        var bbPrev10_Val = bbPrev10.UpperBand - bbPrev10.LowerBand;
                        var ratePrev10 = Math.Round(bbPrev10_Val.Value / avgBB.Value, 2);

                        var bbPrev30 = lbb.Where(x => x.Date < itemSOS.Date).SkipLast(29).Last();
                        var bbPrev30_Val = bbPrev30.UpperBand - bbPrev30.LowerBand;
                        var ratePrev30 = Math.Round(bbPrev30_Val.Value / avgBB.Value, 2);
                        if (ratePrev1 > 0.9 
                            || ratePrev10 > 0.9
                            || ratePrev30 > 0.9)
                            continue;

                        var count = lData.Count(x => x.Date > itemSOS.Date);
                        if (count < 25)//Sau SOS ít nhất 25 nến thì mới kiểm tra các điều kiện
                        {
                            return (false, null, null, null);
                        }
                            
                        var lCheck = lData.Where(x => x.Date > itemSOS.Date).Take(25);
                        var countBelowMa20 = 0;
                        foreach (var item in lCheck.TakeLast(10))
                        {
                            var bbBelow = lbb.First(x => x.Date == item.Date);
                            if(item.Close < (decimal)bbBelow.Sma)
                                countBelowMa20++;
                        }
                        if(countBelowMa20 < 5) return (false, null, null, null);//5/10 nến gần nhất phải < Ma20

                        var closeMax25 = lCheck.Max(x => x.High);
                        var closeMaxC25 = lCheck.Max(x => x.Close);

                        if ((closeMaxC25 - itemSOS.Close) > 5 * (itemSOS.Close - itemSOS.Open)) return (false, null, null, null);//khoảng cách từ điểm Close cao nhất không được quá lớn so với độ dài SOS
                        //if (closeMax25 > (itemSOS.High + (decimal)bbPrev1_Val.Value))//Sau SOS giá tăng quá một mức cụ thể -> loại
                        //    continue;
                        var next25 = lCheck.Last();
                        var SKIP = 25;
                        var bbNext25 = lbb.First(x => x.Date == next25.Date);
                        if (next25.Close > (decimal)bbNext25.Sma.Value)
                        {
                            var next35 = lData.Where(x => x.Date > itemSOS.Date).Skip(34).FirstOrDefault();
                            if(next35 != null)
                            {
                                SKIP = 35;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        foreach (var item in lData.Where(x => x.Date > itemSOS.Date).Skip(SKIP))
                        {
                            var bb_item = lbb.First(x => x.Date == item.Date);
                            if (item.Close > (decimal)bb_item.Sma.Value)
                            {
                                if (item.High < closeMax25)
                                    return (true, itemSOS, item, 0);

                                return (false, null, null, null);
                            }
                        }

                        //var mes = $"SOS: {itemSOS.Date.ToString("dd/MM/yyyy HH")}| {Math.Round(bbPrev1_Val.Value / avgBB.Value, 2)}";
                        //Console.WriteLine(mes);
                        return (false, null, null, null);
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

            return (false, null, null, null);
        }
        /// <summary>
        /// Flag, SOS, ENTRY
        /// Có 2 loại SOS
        /// + Loại 1: Tạo SOS rồi đi sóng ABC, mua khi cắt lên MA và quay lại Test Ma
        /// + Loại 2: Tạo SOS rồi đi xuống MA nhanh chóng quay lại, mua khi vừa cắt lên MA
        /// </summary>
        /// <param name="lData"></param>
        /// <returns></returns>
        public static (bool, Quote, Quote, int?) IsWyckoff_20250808(this IEnumerable<Quote> lData)
        {
            try
            {
                if ((lData?.Count() ?? 0) < 150)
                    return (false, null, null, null);

                var lbb = lData.GetBollingerBands();
                var lrsi = lData.GetRsi();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);
                var lSOS = lData.TakeLast(72);
                var lWyc = new List<Quote>();
                foreach (var itemSOS in lSOS.Where(x => x.Close > x.Open && (x.Close - x.Open) >= 0.4m * (x.High - x.Low)).OrderByDescending(x => x.Date))
                {
                    try
                    {
                        var rateSOS = Math.Round(100 * (-1 + itemSOS.Close / itemSOS.Open), 1);
                        if (rateSOS > 5) continue;//Nến SOS không được vượt quá 7%

                        var rsi = lrsi.First(x => x.Date == itemSOS.Date);
                        if (rsi.Rsi <= 50) continue;//RSI phải lớn hơn 50
                        var ma20Vol = lMaVol.First(x => x.Date == itemSOS.Date);
                        if (itemSOS.Volume <= 2m * (decimal)ma20Vol.Sma.Value) continue; //Vol phải lớn hơn 2 lần MA20

                        var countMaxVolPrevGreenSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(35).Count(x => x.Volume > itemSOS.Volume && x.Close > x.Open);
                        if (countMaxVolPrevGreenSOS >= 1) continue; //Vol phải lớn hơn 35 nến xanh liền trước

                        var countMaxVolPrevSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(10).Count(x => x.Volume > itemSOS.Volume);
                        if (countMaxVolPrevSOS >= 1) continue; //Vol phải lớn hơn 10 nến liền trước

                        var maxClosePrevSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(35).Max(x => x.Close);
                        var minClosePrevSOS = lData.Where(x => x.Date < itemSOS.Date).TakeLast(35).Min(x => x.Close);
                        var maxmin20Prev = maxClosePrevSOS - minClosePrevSOS;
                        
                        if ((itemSOS.Close - itemSOS.Open) > 2.5m * maxmin20Prev) continue;//Độ dài SOS không được lớn hơn 2.5 lần độ rộng của 35 nến trước đó

                        //Check Type 2
                        var lCheckType2 = lData.Where(x => x.Date > itemSOS.Date).Skip(3).Take(12);
                        var flagType2 = 0;//1: nến đầu tiên trên ma20; 2: nến cắt xuống ma20, 3: nến nằm toàn bộ phía dưới ma20; 4: nến cắt lên ma20
                        Quote itemType2 = null;
                        foreach (var item in lCheckType2)
                        {
                            //First check
                            var bb = lbb.First(x => x.Date == item.Date);
                            if (flagType2 == 0
                               && item.Close <= (decimal)bb.Sma.Value)
                                break;

                            if (flagType2 < 1)
                                flagType2 = 1;
                            //Second check
                            if(flagType2 == 1
                                && item.Close < (decimal)bb.Sma.Value)
                            {
                                itemType2 = item;
                                flagType2 = 2;
                                continue;
                            }    
                            //Third check
                            if(flagType2 == 2 
                                && Math.Max(item.Open, item.Close) < (decimal)bb.Sma.Value)
                            {
                                flagType2 = 3;
                                continue;
                            }
                            //Fourth check
                            if (flagType2 == 3)
                            {
                                if(Math.Max(item.Open, item.Close) < (decimal)bb.Sma.Value)
                                {
                                    flagType2 = 4;
                                    continue;
                                }
                                else
                                {
                                    flagType2 = 0;
                                    break;
                                }
                            }
                            //Fifth check
                            if (flagType2 == 4
                                && item.Close > (decimal)bb.Sma.Value)
                            {
                                flagType2 = 5;
                                if(item.Close > itemSOS.Close) //Nếu điểm vào > SOS thì bỏ qua
                                {
                                    flagType2 = 0;
                                }
                                var minClose = lCheckType2.Where(x => x.Date >= itemType2.Date && x.Date <= item.Date).Min(x => x.Close);
                                var maxClose = lCheckType2.Where(x => x.Date >= itemSOS.Date && x.Date <= itemType2.Date).Max(x => x.Close);
                                if ((maxClose - (decimal)bb.Sma.Value) < 1.5m * ((decimal)bb.Sma.Value - minClose))//Khoảng phía trên phải gấp 1.5 lần khoảng phía dưới
                                {
                                    flagType2 = 0;
                                }
                                //if(minClose < itemSOS.Open)//Min Close không được nhỏ hơn Open của SOS
                                //{
                                //    flagType2 = 0;
                                //}
                                //
                                if(item.High > (decimal)bb.UpperBand)//Entry ko được vượt quá biên trên đường BB
                                {
                                    flagType2 = 0;
                                }    

                                itemType2 = item;
                                break;
                            }    
                        }

                        if (flagType2 == 5
                            && itemType2 != lData.Last())
                        {
                            var entry = lData.First(x => x.Date > itemType2.Date);
                            return (true, itemSOS, entry, 2);
                        }

                        var count = lData.Count(x => x.Date > itemSOS.Date);
                        if (count < 25)//Sau SOS ít nhất 25 nến thì mới kiểm tra các điều kiện
                        {
                            return (false, null, null, null);
                        }

                        if (itemSOS.Close < maxClosePrevSOS) continue; //Close phải lớn hơn 35 nến liền trước

                        var lBB_Prev100 = lbb.Where(x => x.Date < itemSOS.Date).TakeLast(100);
                        var maxBB = lBB_Prev100.Max(x => x.UpperBand - x.LowerBand);
                        var minBB = lBB_Prev100.Min(x => x.UpperBand - x.LowerBand);
                        var avgBB = 0.5 * (maxBB + minBB);

                        var bbPrev1 = lbb.Last(x => x.Date < itemSOS.Date);
                        var bbPrev1_Val = bbPrev1.UpperBand - bbPrev1.LowerBand;
                        var ratePrev1 = Math.Round(bbPrev1_Val.Value / avgBB.Value, 2);

                        var bbPrev10 = lbb.Where(x => x.Date < itemSOS.Date).SkipLast(9).Last();
                        var bbPrev10_Val = bbPrev10.UpperBand - bbPrev10.LowerBand;
                        var ratePrev10 = Math.Round(bbPrev10_Val.Value / avgBB.Value, 2);

                        var bbPrev30 = lbb.Where(x => x.Date < itemSOS.Date).SkipLast(29).Last();
                        var bbPrev30_Val = bbPrev30.UpperBand - bbPrev30.LowerBand;
                        var ratePrev30 = Math.Round(bbPrev30_Val.Value / avgBB.Value, 2);
                        if (ratePrev1 > 0.9
                            || ratePrev10 > 0.9
                            || ratePrev30 > 0.9)
                            continue;
                        
                        var lCheck = lData.Where(x => x.Date > itemSOS.Date).Take(25);
                        var countBelowMa20 = 0;
                        foreach (var item in lCheck.TakeLast(10))
                        {
                            var bbBelow = lbb.First(x => x.Date == item.Date);
                            if (item.Close < (decimal)bbBelow.Sma)
                                countBelowMa20++;
                        }
                        if (countBelowMa20 < 5) return (false, null, null, null);//5/10 nến gần nhất phải < Ma20

                        var closeMax25 = lCheck.Max(x => x.High);
                        var closeMaxC25 = lCheck.Max(x => x.Close);

                        if ((closeMaxC25 - itemSOS.Close) > 5 * (itemSOS.Close - itemSOS.Open)) return (false, null, null, null);//khoảng cách từ điểm Close cao nhất không được quá lớn so với độ dài SOS
                        //if (closeMax25 > (itemSOS.High + (decimal)bbPrev1_Val.Value))//Sau SOS giá tăng quá một mức cụ thể -> loại
                        //    continue;

                        var next25 = lCheck.Last();
                        var SKIP = 25;
                        var bbNext25 = lbb.First(x => x.Date == next25.Date);
                        if (next25.Close > (decimal)bbNext25.Sma.Value)
                        {
                            var next35 = lData.Where(x => x.Date > itemSOS.Date).Skip(34).FirstOrDefault();
                            if (next35 != null)
                            {
                                SKIP = 35;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        foreach (var item in lData.Where(x => x.Date > itemSOS.Date).Skip(SKIP))
                        {
                            var bb_item = lbb.First(x => x.Date == item.Date);
                            if (item.Close > (decimal)bb_item.Sma.Value)
                            {
                                if (item.High < closeMax25
                                    && item != lData.Last())
                                {
                                    var entry = lData.First(x => x.Date > item.Date);
                                    return (true, itemSOS, entry, 1);
                                }

                                return (false, null, null, null);
                            }
                        }

                        //var mes = $"SOS: {itemSOS.Date.ToString("dd/MM/yyyy HH")}| {Math.Round(bbPrev1_Val.Value / avgBB.Value, 2)}";
                        //Console.WriteLine(mes);
                        return (false, null, null, null);
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

            return (false, null, null, null);
        }

        public static (bool, Quote) IsWyckoffOut(this Quote val, IEnumerable<Quote> lData)
        {
            try
            {
                var last = lData.Last();
                var cur = lData.SkipLast(1).Last();
                var prev = lData.SkipLast(2).Last();

                var lRsi = lData.GetRsi();
                var lBB = lData.GetBollingerBands();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);

                var count = lData.Count(x => x.Date > val.Date);
                if (count > 90)//giữ tối đa 90 nến
                    return (true, last);

                if (last.Date <= val.Date)
                    return (false, null);

                ////STOPLOSS
                var rate = Math.Round(100 * (-1 + last.Close / val.Close), 2);
                if (rate < -5)
                {
                    last.Open = val.Close * (1 - 0.05m);
                    return (true, last);
                }

                var rateMax = Math.Round(100 * (-1 + (lData.Where(x => x.Date > val.Date).Max(x => x.Close)) / val.Close), 2);
                

                //CUT khi low < bb
                var bbLast = lBB.First(x => x.Date == last.Date);
                if (last.Low < (decimal)bbLast.LowerBand
                    //&& last.Close > val.Close
                    //&& rate >= 5
                    )
                    return (true, last);

                var bbCur = lBB.First(x => x.Date == cur.Date);
                var bbPrev = lBB.First(x => x.Date == prev.Date);
                if (cur.Close < (decimal)bbCur.Sma
                    && prev.Close < (decimal)bbPrev.Sma
                    && rateMax > 5)
                    return (true, last);

                //Best
                var lDataCheck = lData.Where(x => x.Date > val.Date).Select(x => new QuoteWyckoff
                {
                    Date = x.Date,
                    Close = x.Close,
                    Volume = x.Volume,
                    Ma20Vol = (decimal)lMaVol.First(y => y.Date == x.Date).Sma,
                    Ma20 = (decimal)lBB.First(y => y.Date == x.Date).Sma,
                    PrevVol = lData.LastOrDefault(y => y.Date < x.Date)?.Volume ?? 0,
                    Rsi = (decimal)lRsi.First(y => y.Date == x.Date).Rsi,
                });

                var lFilter = lDataCheck.Where(x => x.Volume > 2 * x.Ma20Vol && x.Volume > 1.5m * x.PrevVol);
                if(lFilter.Count() >= 2)
                {
                    var filterLast = lFilter.Last();
                    
                    var filterMax = lFilter.MaxBy(x => x.Rsi);
                    if(filterMax.Rsi > filterLast.Rsi
                        && filterLast.Close > filterMax.Close
                        && filterMax.Rsi > 70
                        && filterLast.Rsi > 70)
                    {
                        return (true, last);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, Quote) IsWyckoffOut2(this Quote val, IEnumerable<Quote> lData)
        {
            try
            {
                var last = lData.Last();
                var cur = lData.SkipLast(1).Last();
                var prev = lData.SkipLast(2).Last();

                var lRsi = lData.GetRsi();
                var lBB = lData.GetBollingerBands();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);

                var count = lData.Count(x => x.Date > val.Date);
                if (count > 30)//giữ tối đa 90 nến
                    return (true, last);

                if (last.Date <= val.Date)
                    return (false, null);

                ////STOPLOSS
                var rate = Math.Round(100 * (-1 + last.Close / val.Close), 2);
                if (rate < -5)
                {
                    last.Open = val.Close * (1 - 0.05m);
                    return (true, last);
                }

                var bbCur = lBB.First(x => x.Date == cur.Date);
                var rsiCur = lRsi.First(x => x.Date == cur.Date);
                if (rsiCur.Rsi > 70)
                    return (true, last);

                if (cur.Close < (decimal)bbCur.Sma)
                    return (true, last);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }
    }
}
