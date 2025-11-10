using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinUtilsPr
{
    public static class GrokSupport
    {
        /// <summary>
        /// Filter khung 1D – ĐÃ XỬ LÝ NẾN NGÀY CHƯA ĐÓNG
        /// </summary>
        public static bool AllowLong(this List<Quote> dailyQuotes)
        {
            if (dailyQuotes == null || dailyQuotes.Count < 22) return false;

            // LUÔN DÙNG NẾN NGÀY ĐÃ ĐÓNG (ngày hôm qua)
            int idx = dailyQuotes.Count - 2; // nến ngày đã đóng hoàn toàn
            var yesterday = dailyQuotes[idx];

            // Tính SMA volume 20 ngày trên nến đã đóng
            var volSmaResult = dailyQuotes
                .Take(dailyQuotes.Count - 1) // bỏ nến ngày hiện tại đang chạy
                .Use(CandlePart.Volume)
                .GetSma(20)
                .ToList();

            decimal avgVol20 = (decimal)(volSmaResult[idx].Sma ?? 0);

            // Điều kiện CHO PHÉP vào lệnh H1 hôm nay:
            // 1. Ngày hôm qua là nến XANH → xu hướng ngày đã quay lên
            // HOẶC
            // 2. Ngày hôm qua là nến ĐỎ NHƯNG volume thấp → bên bán đã hết hơi
            bool yesterdayGreen = yesterday.Close > yesterday.Open;
            bool yesterdayRedLowVol = yesterday.Close < yesterday.Open && yesterday.Volume <= avgVol20 * 1.2m;

            return yesterdayGreen || yesterdayRedLowVol;
        }
        /// <summary>
        /// Kiểm tra xem có tín hiệu LONG (nến tín hiệu - nến 1) tại Quote thứ áp cuối không
        /// </summary>
        /// <param name="quotes">List<Quote> từ Binance H1</param>
        /// <returns>Quote là nến tín hiệu (nến 1) nếu thỏa mãn, ngược lại null</returns>
        public static Quote? GetLongSignalCandle(this List<Quote> quotes)
        {
            if (quotes == null || quotes.Count < 50) return null; // cần ít nhất 50 nến để tính EMA89, BB20, vol20

            // Lấy nến áp cuối (vì nến cuối chưa đóng)
            int idx = quotes.Count - 2;
            var current = quotes[idx];
            var prev = quotes[idx - 1];

            // 1. Tính Bollinger Bands (20, 2)
            var bbResult = quotes.GetBollingerBands(20, 2).ToList();
            var bb = bbResult[idx];
            if (current.Low >= (decimal)bb.LowerBand) return null;

            // 2. Volume > 2.0 x SMA(volume, 20)
            var volResult = quotes.Use(CandlePart.Volume).GetSma(20).ToList();
            decimal avgVol20 = (decimal)(volResult[idx].Sma ?? 0);
            if (current.Volume <= avgVol20 * 2.0m) return null;

            // 3. Nến đỏ: close < open
            if (current.Close >= current.Open) return null;

            // 4. Râu dưới dài >= 1.5 x thân nến
            decimal body = Math.Abs(current.Close - current.Open);
            decimal lowerWick = Math.Max(current.Open, current.Close) - current.Low;
            if (lowerWick < body * 1.5m) return null;

            // 5. Band đang mở rộng (LowerBand dốc xuống mạnh hơn nến trước)
            var bbPrev = bbResult[idx - 1];
            if (bb.LowerBand >= bbPrev.LowerBand) return null; // LowerBand không giảm

            // 6. Giá < EMA89
            var ema89Result = quotes.GetEma(89).ToList();
            var ema89 = ema89Result[idx].Ema;
            if (ema89 == null || current.Close >= (decimal)ema89) return null;

            // 7. (Tùy chọn nâng cao) Kiểm tra nến ngày để filter – nếu bạn có dữ liệu 1D
            // Bỏ qua nếu chưa tích hợp, hoặc thêm sau

            // TẤT CẢ ĐIỀU KIỆN ĐỒNG THỜI → TRẢ VỀ NẾN TÍN HIỆU
            return current;
        }

        public static (Quote? Candle, SignalStrength Strength) GetLongSignalWithStrength(this List<Quote> quotes)
        {
            var candle = quotes.GetLongSignalCandle();
            if (candle == null) return (null, SignalStrength.None);

            int idx = quotes.Count - 2;
            decimal body = Math.Abs(candle.Close - candle.Open);
            decimal lowerWick = Math.Max(candle.Open, candle.Close) - candle.Low;
            decimal ratio = body == 0 ? 999 : lowerWick / body;

            SignalStrength strength = ratio >= 3.0m ? SignalStrength.Super :
                                      ratio >= 1.5m ? SignalStrength.Good :
                                                      SignalStrength.Normal;

            return (candle, strength);
        }
    }
}
