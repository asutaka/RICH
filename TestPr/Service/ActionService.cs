using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TestPr.Utils;

namespace TestPr.Service
{
    public static class ActionService
    {
        public static (bool, Quote) IsFlagBuy(this List<Quote> lData)
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
                if (e_Sig.Close >= e_Sig.Open
                    || e_Sig.Low >= (decimal)bb_Pivot.LowerBand.Value
                    || e_Sig.Close - (decimal)bb_Pivot.LowerBand.Value >= (decimal)bb_Pivot.Sma.Value - e_Sig.Close
                    || e_Sig.Volume < (decimal)(vol_Sig.Sma.Value * 1.5)
                    || rsi_Sig.Rsi > 35
                    )
                    return (false, null);

                //Check Pivot 
                if (e_Pivot.Low >= (decimal)bb_Pivot.LowerBand.Value
                    || e_Pivot.High >= (decimal)bb_Pivot.Sma.Value
                    || rsi_Pivot.Rsi > 35
                    || rsi_Pivot.Rsi < 25
                    )
                    return (false, null);

                //Check độ dài nến Sig - Pivot
                var body_Sig = Math.Abs((e_Sig.Open - e_Sig.Close) / (e_Sig.High - e_Sig.Low));
                if (body_Sig > (decimal)0.8)
                {
                    var isValid = Math.Abs(e_Pivot.Open - e_Pivot.Close) >= Math.Abs(e_Sig.Open - e_Sig.Close);
                    if (isValid)
                        return (false, null);
                }

                //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                var rateVol = Math.Round(e_Pivot.Volume / e_Sig.Volume, 1);
                if (rateVol > (decimal)0.6) 
                    return (false, null);

                var checkTop = lData.Where(x => x.Date <= e_Pivot.Date).ToList().IsExistTopB();
                if (!checkTop.Item1)
                    return (false, null);

                return (true, e_Pivot);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (false, null);
        }
    }
}
