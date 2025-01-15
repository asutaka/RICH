using Skender.Stock.Indicators;
using StockPr.DAL.Entity;
using StockPr.Model;

namespace StockPr.Utils
{
    public static class IndicatorExtention
    {
        public static (bool, OrderBlock) IsOrderBlock(this Quote item, IEnumerable<OrderBlock> lOrderBlock, long mintime = 86400 * 10)
        {
            try
            {
                if(!(lOrderBlock?.Any() ?? false))
                    return (false, null);

                var top = lOrderBlock.FirstOrDefault(x => (x.Mode == (int)EOrderBlockMode.TopPinbar || x.Mode == (int)EOrderBlockMode.TopInsideBar)
                                                    && item.Close >= x.Focus && item.Close < x.SL && (item.Date - x.Date).TotalSeconds >= mintime);
                if(top != null)
                    return (true, top);

                var bot = lOrderBlock.FirstOrDefault(x => (x.Mode == (int)EOrderBlockMode.BotPinbar || x.Mode == (int)EOrderBlockMode.BotPinbar)
                                                   && item.Close <= x.Focus && item.Close > x.SL && (item.Date - x.Date).TotalSeconds >= mintime);

                if (bot != null)
                    return (true, bot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PartternService.CheckBatDay|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public static List<OrderBlock> GetOrderBlock(this List<Quote> lData, int minrate)
        {
            var lOrderBlock = new List<OrderBlock>();
            try
            {
                var lPivot = lData.GetTopBottom_H(minrate);
                var max = lPivot.Where(x => x.IsTop).MaxBy(x => x.Value);
                var min = lPivot.Where(x => x.IsBot).MinBy(x => x.Value);
                if(max is null || min is null)
                    return lOrderBlock;

                var flagDate = max.Date > min.Date ? min.Date : max.Date;
                lPivot = lPivot.Where(x => x.Date >= flagDate).ToList();
                foreach (var pivot in lPivot)
                {
                    var item = lData.First(x => x.Date == pivot.Date);
                    var len = item.High - item.Low;
                    var avgLen = lData.Where(x => x.Date <= pivot.Date).TakeLast(5).Average(x => x.High - x.Low);
                    if (len < avgLen * (decimal)1.3) 
                        continue;

                    if (pivot.IsTop)
                    {
                        if (pivot.Date < max.Date)
                            continue;

                        var uplen = item.High - Math.Max(item.Open, item.Close);
                        if (uplen / len >= (decimal)0.6)
                        {
                            var entry = item.High - uplen / 4;
                            var sl = entry + uplen;
                            lOrderBlock.Add(new OrderBlock
                            {
                                Date = item.Date,
                                Open = item.Open,
                                Close = item.Close,
                                High = item.High,
                                Low = item.Low,
                                Mode = (int)EOrderBlockMode.TopPinbar,
                                Entry = entry,
                                SL = sl,
                                Focus = Math.Max(item.Open, item.Close),
                            });
                            //Console.WriteLine($"TOP(pinbar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                        }
                        else
                        {
                            if (pivot.Date < min.Date)
                                continue;

                            var next = lData.FirstOrDefault(x => x.Date > pivot.Date);
                            if (next.Open > next.Close
                                && next.Close <= Math.Min(item.Open, item.Close)
                                && next.Open >= Math.Max(item.Open, item.Close))
                            {
                                var entry = Math.Min(item.Open, item.Close) + 3 * Math.Abs(item.Open - item.Close) / 4;
                                var sl = Math.Max(item.High, next.High) + Math.Abs(item.Open - item.Close) / 4;
                                lOrderBlock.Add(new OrderBlock
                                {
                                    Date = item.Date,
                                    Open = item.Open,
                                    Close = item.Close,
                                    High = item.High,
                                    Low = item.Low,
                                    Mode = (int)EOrderBlockMode.TopInsideBar,
                                    Entry = entry,
                                    SL = sl,
                                    Focus = Math.Min(item.Open, item.Close)
                                });
                                //Console.WriteLine($"TOP(outsidebar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                            }
                        }
                    }
                    else
                    {
                        var belowLen = Math.Min(item.Open, item.Close) - item.Low;
                        if (belowLen / len >= (decimal)0.6)
                        {
                            var entry = belowLen / 4 + item.Low;
                            var sl = entry - belowLen;
                            //Console.WriteLine($"BOT(pinbar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                            lOrderBlock.Add(new OrderBlock
                            {
                                Date = item.Date,
                                Open = item.Open,
                                Close = item.Close,
                                High = item.High,
                                Low = item.Low,
                                Mode = (int)EOrderBlockMode.BotPinbar,
                                Entry = entry,
                                SL = sl,
                                Focus = Math.Max(item.Open, item.Close)
                            });
                        }
                        else
                        {
                            var prev = lData.LastOrDefault(x => x.Date < pivot.Date);
                            if (prev is null || prev.Open <= prev.Close || item.Open >= item.Close)
                                continue;

                            if (item.Close >= Math.Max(prev.Open, prev.Close)
                                && item.Low <= prev.Low)
                            {
                                var entry = Math.Min(prev.Open, prev.Close) + Math.Abs(prev.Open - prev.Close) / 4;
                                var sl = Math.Min(item.Low, prev.Low) + Math.Abs(prev.Open - prev.Close) / 4; ;
                                lOrderBlock.Add(new OrderBlock
                                {
                                    Date = item.Date,
                                    Open = item.Open,
                                    Close = item.Close,
                                    High = item.High,
                                    Low = item.Low,
                                    Mode = (int)EOrderBlockMode.BotInsideBar,
                                    Entry = entry,
                                    SL = sl,
                                    Focus = Math.Max(item.Open, item.Close)
                                });
                                //Console.WriteLine($"BOT(outsidebar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"IndicatorExtention.GetOrderBlock|EXCEPTION| {ex.Message}");
            }
            return lOrderBlock;
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
                            if(last.IsBot)
                            {
                                if(last.Value > model.Value)
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
            catch(Exception ex)
            {
                Console.WriteLine($"IndicatorExtention.GetTopBottom_HL|EXCEPTION| {ex.Message}");
            }
            return lResult;
        }
    }
}
