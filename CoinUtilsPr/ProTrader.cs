using CoinUtilsPr.Model;
using Skender.Stock.Indicators;

namespace CoinUtilsPr
{
    public static class ProTrader
    {
        public static ProModel? GetRealEntry(this ProModel entity, IEnumerable<Quote> quotes)
        {
            try
            {
                var lrsi = quotes.GetRsi(14).ToList(); 
                var lma9 = lrsi.GetSma(9).ToList();
                var lwma45 = lrsi.GetWma(45).ToList();

                var rsi_Cur = lrsi.First(x => x.Date == entity.entity.Date);
                var lcheck = lrsi.Where(x => x.Date > entity.entity.Date);
                var index = 1;
                foreach ( var x in lcheck )
                {
                    if (index > 5)
                        return null;

                    if(x.Rsi < rsi_Cur.Rsi)
                    {
                        return entity;
                    }
                    index++;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("GetEntry error: " + ex.Message);
            }
            return null;
        }
        public static ProModel? GetEntry(this List<Quote> quotes)
        {
            try
            {
                if (quotes.Count < 50) return null;

                var lbb = quotes.GetBollingerBands().ToList();
                var bbCur = lbb[^2];

                // SỬA LỖI 1: công thức BB width đúng
                decimal bbWidth = bbCur.LowerBand.HasValue && bbCur.LowerBand != 0
                ? 100m * ((decimal)bbCur.UpperBand! - (decimal)bbCur.LowerBand!) / (decimal)bbCur.LowerBand!
                : 0m;
                if (bbWidth < 1.8m) return null;

                var lrsi = quotes.GetRsi(14).ToList(); 
                if (lrsi.Count < 46) return null;

                var lma9 = lrsi.GetSma(9).ToList();
                var lwma45 = lrsi.GetWma(45).ToList();

                // RSI – ép kiểu double? → decimal
                decimal rsiCur = lrsi[^2].Rsi.HasValue ? (decimal)lrsi[^2].Rsi.Value : 50m;
                decimal rsiPrev = lrsi[^3].Rsi.HasValue ? (decimal)lrsi[^3].Rsi.Value : 50m;

                // MA – decimal + ép kiểu
                decimal rsiMA9 = (decimal)(lma9[^2].Sma ?? 0);
                decimal rsiWMA45 = (decimal)(lwma45[^2].Wma ?? 0);
                decimal rsiMA9Prev = (decimal)(lma9[^3].Sma ?? 0);
                decimal rsiWMA45Prev = (decimal)(lwma45[^3].Wma ?? 0);

                bool buy1 = rsiCur > rsiMA9 && rsiPrev <= rsiMA9Prev;
                bool buy2 = rsiCur > rsiWMA45 && rsiPrev <= rsiWMA45Prev;

                // SỬA LỖI 3: entity đúng nến đã đóng
                var candle = quotes[^2];

                var output = new ProModel
                {
                    entity = candle,
                    sl = candle.Close * 0.985m
                };

                if (buy1 && rsiCur < 45m)
                {
                    output.Strength = (int)SignalStrength.Early;
                    output.riskPercent = 1.5m;
                }
                else if (buy2 && rsiCur < 35m)
                {
                    output.Strength = (int)SignalStrength.Super;
                    output.riskPercent = 3.5m;
                }
                else if (buy2)
                {
                    output.Strength = (int)SignalStrength.Confirm;
                    output.riskPercent = 2.8m;
                }
                else return null;

                output.sl = candle.Close * (1 - output.riskPercent / 100);

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEntry error: " + ex.Message);
                return null;
            }
        }

        public static Quote GetEntryCombo(this List<Quote> quotes)
        {
            try
            {
                var cur = quotes[^2];
                var lrsi = quotes.GetRsi().ToList();
                var lma9 = lrsi.GetSma(9).ToList();
                var lwma45 = lrsi.GetWma(45).ToList();

                var count = lrsi.Count();
                if (count < 100)
                    return null;

                var index = count - 90;
                var soBupSong = 0;
                var soLanXoan = 0;
                for (int i = index; i < count; i++)
                {
                    var rsi = lrsi[i].Rsi ?? 0;
                    var ma9 = lma9[i].Sma ?? 0;
                    var wma45 = lwma45[i].Wma ?? 0;
                    if (rsi == 0 || ma9 == 0 || wma45 == 0)
                        continue;

                    var distance = MaxDistance(rsi, ma9, wma45);
                    if (soBupSong < 10)//xd sóng giảm
                    {
                        if (rsi <= 50 && distance >= 10)
                        {
                            soBupSong++;
                            continue;
                        }

                        soBupSong = 0;
                    }
                    else if (soLanXoan < 4)//xd điểm xoắn
                    {
                        if (distance <= 4)
                        {
                            soLanXoan++;
                            continue;
                        }

                        soLanXoan = 0;
                    }
                    else if (rsi > ma9) 
                    {
                        return cur;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }

            double MaxDistance(double rsi, double ma9, double wma45)
            {
                double d1 = Math.Abs(rsi - ma9);
                double d2 = Math.Abs(rsi - wma45);
                double d3 = Math.Abs(ma9 - wma45);
                return Math.Max(d1, Math.Max(d2, d3));
            }

            return null;
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
