using CoinUtilsPr.DAL.Entity;
using CoinUtilsPr.Model;
using Skender.Stock.Indicators;

namespace CoinUtilsPr
{
    public static class ProTrader
    {
        public static string GetOut(this List<TakerVolumneBuySellDTO> takervolumes)
        {
            try
            {
                var count = takervolumes.Count;
                if (count < 2) return null;

                var prev = takervolumes[count - 2];
                var cur = takervolumes[count - 1];

                var down = Math.Round(prev.buySellRatio / cur.buySellRatio, 1);
                if (down > 1.3m)
                {
                    var time = ((long)(cur.timestamp)).UnixTimeStampMinisecondToDateTime();
                    var mesPivot = $"DOWN:{time.ToString("dd/MM HH:mm")}";
                    Console.WriteLine(mesPivot);
                    return mesPivot;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEntry error: " + ex.Message);
            }
            return string.Empty;
        }
        public static TakerVolumneBuySellDTO? GetSignal(this List<TakerVolumneBuySellDTO> takervolumes, List<Quote> quotes)
        {
            try
            {
                var count = takervolumes.Count;
                if (count < 2) return null;

                var prev = takervolumes[count - 2];
                var cur = takervolumes[count - 1];
                var up = Math.Round(cur.buySellRatio / prev.buySellRatio, 1);
                
                if (up > 1.3m)
                {
                    var lrsi = quotes.GetRsi();
                    var lma9 = lrsi.GetSma(9);
                    var lbb = quotes.GetBollingerBands();
                    var time = ((long)(cur.timestamp)).UnixTimeStampMinisecondToDateTime();
                    var rsi = lrsi.First(x => x.Date == time);
                    var ma9 = lma9.First(x => x.Date == time);
                    var bb_check = lbb.Where(x => x.Date < time).TakeLast(20).MaxBy(x => x.UpperBand - x.LowerBand);
                    var rate = Math.Round(100 * (-1 + bb_check.UpperBand.Value / bb_check.LowerBand.Value), 2);
                    if (rsi.Rsi.Value < 45
                        && rsi.Rsi.Value < ma9.Sma.Value
                        && rate < 10)
                    {
                        Console.WriteLine($"====> SIGNAL: {time.Date.ToString("dd/MM")} {time.Hour.To2Digit()}:{time.Minute.To2Digit()}");
                        return cur;
                    }
                }

                //var down = Math.Round(prev.buySellRatio / cur.buySellRatio, 1);
                //if (down > 1.3m)
                //{
                //    var time = ((long)(cur.timestamp)).UnixTimeStampMinisecondToDateTime();
                //    var mesPivot = $"DOWN:{time.ToString("dd/MM HH:mm")}";
                //    Console.WriteLine(mesPivot);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEntry error: " + ex.Message);
            }
            return null;
        }
        public static (int, Quote) GetEntry(this List<Quote> quotes)
        {
            try
            {
                var lrsi = quotes.GetRsi().ToList();
                var lma9 = lrsi.GetSma(9).ToList();
                var lwma45 = lrsi.GetWma(45);
                var lbb = quotes.GetBollingerBands();

                var cur = quotes[^1];
                var bb = lbb.First(x => x.Date == cur.Date);
                var rsi = lrsi.First(x => x.Date == cur.Date);
                var ma9 = lma9.First(x => x.Date == cur.Date);
                var wma45 = lwma45.First(x => x.Date == cur.Date);
                
                if (rsi.Rsi.Value >= ma9.Sma.Value) 
                {
                    //Entry không được vượt ma20
                    if (cur.High >= (decimal)bb.Sma) 
                    {
                        //Console.WriteLine($"[LOAI]|{cur.Date.ToString("dd/MM HH:mm")}|Do nen vuot MA20");
                        return (0, null);
                    } 
                    //bbwidth không được quá lớn
                    var maxbbwidth = lbb.Where(x => x.Date < cur.Date).TakeLast(10).MaxBy(x => x.UpperBand.Value - x.LowerBand.Value);
                    var rate_maxbbwidth = Math.Round((-1 + maxbbwidth.UpperBand.Value/maxbbwidth.LowerBand.Value),3);
                    if(rate_maxbbwidth > 0.07)
                    {
                        var rate_bb = Math.Round((-1 + bb.UpperBand.Value / bb.LowerBand.Value), 3);
                        if (rate_bb / rate_maxbbwidth > 0.9) 
                        {
                            //Console.WriteLine($"[LOAI]|{cur.Date.ToString("dd/MM HH:mm")}|Do BB Width qua rong");
                            return (0, null);
                        } 
                    }
                    //Nếu từ điểm ma9 cắt xuống wma45 > 24 thanh thì check độ rộng không được vượt quá 0.4 lần độ rộng lớn nhất
                    var lma9_check = lma9.Where(x => x.Date < cur.Date).TakeLast(50).OrderByDescending(x => x.Date).ToList();
                    var lwma45_check = lwma45.Where(x => x.Date < cur.Date).TakeLast(50).OrderByDescending(x => x.Date).ToList();
                    var count_check = lma9_check.Count();
                    var isSoDiemDuoiVuotToiDa = false;
                    for ( var i = 0; i < count_check; i++ )
                    {
                        var ma9_check = lma9_check[i];
                        var wma45_check = lwma45_check[i];
                        if(ma9_check.Sma.Value > wma45_check.Wma.Value)
                        {
                            var sodiemduoi_wma45 = lma9.Count(x => x.Date > ma9_check.Date);
                            if(sodiemduoi_wma45 >= 24)
                            {
                                isSoDiemDuoiVuotToiDa = true;
                            }
                            break;
                        }
                    }
                    if (isSoDiemDuoiVuotToiDa)
                    {
                        double maxDiv = -1;
                        for (var i = 0; i < count_check; i++)
                        {
                            var ma9_check = lma9_check[i];
                            var wma45_check = lwma45_check[i];
                            var div = wma45_check.Wma.Value - ma9_check.Sma.Value;
                            if(div > maxDiv)
                            {
                                maxDiv = div;
                            }
                        }
                        var div_cur = wma45.Wma.Value - ma9.Sma.Value;
                        if(div_cur / maxDiv > 0.4)
                        {
                            //Console.WriteLine($"[LOAI]|{cur.Date.ToString("dd/MM HH:mm")}|Do nam trong bup qua dai");
                            return (0, null);
                        }
                    }

                    return (1, cur);
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEntry error: " + ex.Message);
            }
            return (-1, null);
        }
        public static Pro? GetEntry(this List<Quote> quotes, EInterval interval)
        {
            try
            {
                var sl = 1.2m;
                if (quotes.Count < 50) return null;
                var lMaVol = quotes.Use(CandlePart.Volume).GetSma(20).ToList();
                var maVolPrev = lMaVol[^3];
                var maVolPrev_2 = lMaVol[^4];

                var cur = quotes[^2];
                var prev = quotes[^3];
                var prev_2 = quotes[^4];
                if ((prev.Volume * 1.5m <= prev_2.Volume && prev_2.Open > prev_2.Close && prev.Close > prev.Open && prev_2.Volume > (decimal)maVolPrev_2.Sma)
                    || (cur.Volume * 1.5m <= prev.Volume && prev.Open > prev.Close && prev.Volume > (decimal)maVolPrev.Sma))
                {
                    return null;//Tất cả những case này đều hỏng
                }

               
                var eMaxVol = quotes.SkipLast(1).TakeLast(14).Where(x => x.Close < x.Open).MaxBy(x => x.Volume);
                if(eMaxVol != null && (decimal)lMaVol.First(x => x.Date == eMaxVol.Date).Sma * 1.5m < eMaxVol.Volume)
                {
                    var eMaMaxVol = lMaVol.First(x => x.Date == eMaxVol.Date);
                    if ((decimal)eMaMaxVol.Sma * 3 < eMaxVol.Volume)
                    {
                        return null;//Vol giảm quá lớn 
                    }

                }

                var lbb = quotes.GetBollingerBands().ToList();
                var bbCur = lbb[^2];

                decimal bbWidth = bbCur.LowerBand.HasValue && bbCur.LowerBand != 0
                ? 100m * ((decimal)bbCur.UpperBand! - (decimal)bbCur.LowerBand!) / (decimal)bbCur.LowerBand!
                : 0m;

                if (cur.Close >= (decimal)bbCur.Sma) return null;
                if(interval == EInterval.H1)
                {
                    if (bbWidth < 1.8m) return null;
                }
                else if (interval == EInterval.M15)
                {
                    if (bbWidth < 1.5m) return null;
                }
                else if (interval == EInterval.M5)
                {
                    if (bbWidth < 1m) return null;
                }

                var lrsi = quotes.GetRsi().ToList();
                if (lrsi.Count < 46) return null;

                var lma9 = lrsi.GetSma(9).ToList();
                var lwma45 = lrsi.GetWma(45).ToList();

                decimal rsiCur = lrsi[^2].Rsi.HasValue ? (decimal)lrsi[^2].Rsi.Value : 50m;
                decimal rsiPrev = lrsi[^3].Rsi.HasValue ? (decimal)lrsi[^3].Rsi.Value : 50m;

                decimal rsiMA9 = (decimal)(lma9[^2].Sma ?? 0);
                decimal rsiMA9Prev = (decimal)(lma9[^3].Sma ?? 0);

                decimal rsiWMA45 = (decimal)(lwma45[^2].Wma ?? 0);

                bool buy1 = rsiCur > rsiMA9 && rsiPrev <= rsiMA9Prev;
                

                var output = new Pro
                {
                    entity = cur,
                };

                if (buy1 && rsiCur < 45m){}
                else return null;

                output.sl_price = cur.Low * (1 - sl / 100);

                var rsi_30 = lrsi.SkipLast(6).TakeLast(30).MinBy(x => x.Rsi);
                //rsi_30 phải nhỏ hơn 40 
                //var day_2 = false;
                if(rsi_30.Rsi < 40)
                {
                    var rsi_31 = lrsi.First(x => x.Date >  rsi_30.Date);
                    var rsi_29 = lrsi.Last(x => x.Date <  rsi_30.Date);
                    if (rsi_30.Rsi <= Math.Min((double)rsi_31.Rsi, (double)rsi_29.Rsi))
                    {
                        //day_2 = true;
                    }
                    else return null;//Phải là đáy 2 
                }

                //Tại thời điểm break thì số nến tăng tối đa là 2
                var countBreak = quotes.Where(x => x.Date < output.entity.Date).TakeLast(3).Count(x => x.Close > x.Open);
                if(countBreak >= 3)
                {
                    return null;
                }
                //Tỉ lệ vào lệnh
                /*
                    + Cắt MA9: 40%
                    + Cắt WMA45: 40%
                    + MA9 > WMA45: 20%
                    + Pos > 1/2(MA20 - LOWER): 30%
                 */
                output.ratio = 50;
                if(cur.Close > rsiWMA45)
                {
                    output.ratio += 30;
                }
                if(rsiMA9 > rsiWMA45)
                {
                    output.ratio += 20;
                }
                if(Math.Abs((decimal)bbCur.Sma - cur.Close) < Math.Abs(cur.Close - (decimal)bbCur.LowerBand))
                {
                    output.ratio = 50;
                }
                output.sl_rate = sl;
                if (interval == EInterval.M15)
                {
                    output.ratio -= 20;
                }
                output.interval = (int)interval;

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEntry error: " + ex.Message);
                return null;
            }
        }

        //    public static void TakeProfit(this List<Quote> quotes, ProModel entry)
        //    {
        //        try
        //        {
        //            if (entry?.entity == null) return;

        //            var current = quotes[^2]; // nến đã đóng realtime
        //            decimal priceNow = current.Close;
        //            decimal entryPrice = entry.entity.Close;

        //            // Tính BB realtime
        //            var bb = quotes.GetBollingerBands(20, 2).ToList()[^2];
        //            double upperBand = bb.UpperBand.Value;
        //            double ma20 = bb.Sma.Value;

        //            // Tính Fib 1.618 cho BUY3
        //            decimal recentHigh = quotes.TakeLast(50).Max(q => q.High);
        //            decimal recentLow = quotes.TakeLast(50).Min(q => q.Low);
        //            decimal fib1618 = recentLow + (recentHigh - recentLow) * 1.618m;

        //            switch ((SignalStrength)entry.Strength)
        //            {
        //                case SignalStrength.Super: // BUY3 – SIÊU MẠNH
        //                    Console.WriteLine("TP SIÊU MẠNH – 3 LỚP VÀNG");
        //                    Console.WriteLine($"TP1 (40%): MA20      = {ma20:F1}$ → {(priceNow >= ma20 ? "ĐÃ CHẠM" : "CÒN " + (ma20 - priceNow):F1}$")}");
        //            Console.WriteLine($"TP2 (40%): UpperBand = {upperBand:F1}$ → {(priceNow >= upperBand*0.999m ? "CHẠM → CHỐT 80% NGAY!" : "CÒN " + (upperBand-priceNow):F1}$")}");
        //            Console.WriteLine($"TP3 (20%): Fib 1.618 = {fib1618:F1}$ → Trail đến đây");
        //            if (priceNow >= upperBand*0.999m)
        //                Console.WriteLine("CẢNH BÁO: CHẠM UPPERBAND → CHỐT 80% NGAY, ĐỪNG ĐỢI FIB!");
        //                    break;

        //                case SignalStrength.Confirm: // BUY2 – XÁC NHẬN
        //                    Console.WriteLine("TP CONFIRM – 2 LỚP BẠC");
        //                    Console.WriteLine($"TP1 (60%): MA20      = {ma20:F1}$ → {(priceNow >= ma20 ? "ĐÃ CHẠM" : "CÒN " + (ma20 - priceNow):F1}$")}");
        //            Console.WriteLine($"TP2 (40%): UpperBand = {upperBand:F1}$ → {(priceNow >= upperBand*0.999m ? "CHẠM → CHỐT HẾT 100%!" : "CÒN " + (upperBand-priceNow):F1}$")}");
        //            if (priceNow >= upperBand*0.999m)
        //                Console.WriteLine("CẢNH BÁO: CHẠM UPPERBAND → CHỐT HẾT NGAY, KHÔNG TRAIL!");
        //                    break;

        //                case SignalStrength.Early: // BUY1 – SỚM
        //                    Console.WriteLine("TP SỚM – CHỈ 1 LỚP");
        //                    Console.WriteLine($"TP DUY NHẤT: MA20 = {ma20:F1}$ → {(priceNow >= ma20 ? "ĐÃ CHẠM → CHỐT HẾT!" : "CÒN " + (ma20 - priceNow):F1}$")}");
        //            break;
        //    }


        //            Console.WriteLine($"SL: {entry.sl:F1}$ | Rủi ro: ${entry.risk:F2} | BB width: {100m * (upperBand - bb.LowerBand) / bb.LowerBand:F2}%");
        //                    Console.WriteLine("==========================================");
        //            }
        //catch (Exception ex)
        //        {
        //            Console.WriteLine("Lỗi TakeProfit: " + ex.Message);
        //        }
        //    }
    }
}
