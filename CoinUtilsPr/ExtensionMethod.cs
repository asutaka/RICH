using CoinUtilsPr.Model;
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

        public static (bool, Quote) IsBuy(this Quote val, decimal close, EOrderSideOption op = EOrderSideOption.OP_0)
        {
            try
            {
                decimal ENTRY_RATE = DetectOption(op);
                decimal LEN = 2.5m;
                decimal SL_PRICE = close * (1 - ENTRY_RATE / 100);
                var rateCheck = Math.Round(100 * (-1 + val.Low / close), 1);
                if (rateCheck <= -ENTRY_RATE)
                {
                    //var dodainen = Math.Abs(Math.Round(100 * (-1 + SL_PRICE / val.Open), 1));
                    //if (dodainen >= LEN)
                    //    return (false, null);

                    val.Close = SL_PRICE;

                    return (true, val);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, Quote) IsFlagBuy(this List<Quote> lData)
        {
            decimal BB_Min = 1m;
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
                        //&& rsi_Pivot.Rsi.Value >= 25
                        //&& rsi_Pivot.Rsi.Value <= 35
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

                return (true, e_Pivot);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static decimal IsBuy2(this Quote val, Quote e_Pivot)
        {
            try
            {
                return val.Close > Math.Max(e_Pivot.Open, e_Pivot.Close) ? -1 : val.Close;
                //return val.Low < e_Pivot.Close ? e_Pivot.Close : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return -1;
        }

        private static decimal DetectOption(EOrderSideOption op)
        {
            if (op == EOrderSideOption.OP_1)
            {
                return 1.5m;
            }
            else if (op == EOrderSideOption.OP_2)
            {
                return 1m;
            }
            else if (op == EOrderSideOption.OP_3)
            {
                return 0.5m;
            }
            else if (op == EOrderSideOption.OP_4)
            {
                return 0m;
            }
            return 2.5m;
        }

        public static (bool, Quote) IsFlagSell(this List<Quote> lData)
        {
            try
            {
                if ((lData?.Count() ?? 0) < 50)
                    return (false, null);

                var lbb = lData.GetBollingerBands();
                var lrsi = lData.GetRsi();
                var lMaVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).GetSma(20);

                var e_Cur = lData.Last();

                var e_Pivot = lData.SkipLast(1).Last();
                var rsi_Pivot = lrsi.First(x => x.Date == e_Pivot.Date);
                var bb_Pivot = lbb.First(x => x.Date == e_Pivot.Date);
                var vol_Pivot = lMaVol.First(x => x.Date == e_Pivot.Date);

                var e_Sig = lData.SkipLast(2).Last();
                var rsi_Sig = lrsi.First(x => x.Date == e_Sig.Date);
                var bb_Sig = lbb.First(x => x.Date == e_Sig.Date);
                var vol_Sig = lMaVol.First(x => x.Date == e_Sig.Date);

                //Check Sig
                if (e_Sig.Close <= e_Sig.Open
                    || e_Sig.High <= (decimal)bb_Pivot.UpperBand.Value
                    || (decimal)bb_Pivot.UpperBand.Value - e_Sig.Close >= e_Sig.Close - (decimal)bb_Pivot.Sma.Value
                    || e_Sig.Volume < (decimal)(vol_Sig.Sma.Value * 1.5)
                    || rsi_Sig.Rsi < 65
                    )
                    return (false, null);

                //Check Pivot 
                if (e_Pivot.High <= (decimal)bb_Pivot.UpperBand.Value
                    || e_Pivot.Low <= (decimal)bb_Pivot.Sma.Value
                    || rsi_Pivot.Rsi > 80
                    || rsi_Pivot.Rsi < 65
                    )
                    return (false, null);

                //check div by zero
                if (e_Sig.High == e_Sig.Low
                    || e_Pivot.High == e_Pivot.Low
                    || Math.Min(e_Pivot.Open, e_Pivot.Close) == e_Pivot.Low)
                    return (false, null);

                //Check độ dài nến Sig - Pivot
                var body_Sig = Math.Abs((e_Sig.Open - e_Sig.Close) / (e_Sig.High - e_Sig.Low));
                var body_Pivot = Math.Abs((e_Pivot.Open - e_Pivot.Close) / (e_Pivot.High - e_Pivot.Low));  //độ dài nến pivot
                var isHammer = (e_Sig.High - e_Sig.Close) >= (decimal)1.2 * (e_Sig.Close - e_Sig.Low);

                if (isHammer)
                {

                }
                else if (body_Pivot < (decimal)0.2)
                {
                    var checkDoji = (e_Pivot.High - Math.Max(e_Pivot.Open, e_Pivot.Close)) / (Math.Min(e_Pivot.Open, e_Pivot.Close) - e_Pivot.Low);
                    if (checkDoji >= (decimal)0.75 && checkDoji <= (decimal)1.25)
                    {
                        return (false, null);
                    }
                }
                else if (body_Sig > (decimal)0.8)
                {
                    var isValid = Math.Abs(e_Pivot.Open - e_Pivot.Close) >= Math.Abs(e_Sig.Open - e_Sig.Close);
                    if (isValid)
                        return (false, null);
                }

                //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                var rateVol = Math.Round(e_Pivot.Volume / e_Sig.Volume, 1);
                if (rateVol > (decimal)0.6)
                    return (false, null);

                //var checkTop = lData.Where(x => x.Date <= e_Pivot.Date).ToList().IsExistBotB();
                //if (!checkTop.Item1)
                //    return (false, null);

                return (true, e_Pivot);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }

        public static (bool, Quote) IsSell(this Quote val, decimal close, EOrderSideOption op = EOrderSideOption.OP_0)
        {
            try
            {
                decimal ENTRY_RATE = DetectOption(op);
                decimal LEN = 2.5m;
                decimal SL_PRICE = close * (1 + ENTRY_RATE / 100);
                var rateCheck = Math.Round(100 * (-1 + val.High / close), 1);
                if (rateCheck >= ENTRY_RATE)
                {
                    //var dodainen = Math.Abs(Math.Round(100 * (-1 + SL_PRICE / val.Open), 1));
                    //if (dodainen >= LEN)
                    //    return (false, null);

                    val.Close = SL_PRICE;

                    return (true, val);
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
