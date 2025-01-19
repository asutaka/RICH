using Skender.Stock.Indicators;
using TestPr.Model;

namespace TestPr.Utils
{
    public static class IndicatorExtention
    {
        public static (bool, OrderBlock) IsOrderBlock(this Quote item, IEnumerable<OrderBlock> lOrderBlock, long mintime = 24 * 10)
        {
            try
            {
                if (!(lOrderBlock?.Any() ?? false))
                    return (false, null);

                var top = lOrderBlock.FirstOrDefault(x => (x.Mode == (int)EOrderBlockMode.TopPinbar || x.Mode == (int)EOrderBlockMode.TopInsideBar)
                                                    && item.Close >= x.Focus && item.Close < x.SL && (item.Date - x.Date).TotalHours >= mintime);
                if (top != null)
                    return (true, top);

                var bot = lOrderBlock.FirstOrDefault(x => (x.Mode == (int)EOrderBlockMode.BotPinbar || x.Mode == (int)EOrderBlockMode.BotPinbar)
                                                   && item.Close <= x.Focus && item.Close > x.SL && (item.Date - x.Date).TotalHours >= mintime);

                if (bot != null)
                    return (true, bot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PartternService.IsOrderBlock|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public static (bool, IEnumerable<OrderBlock>) IsOrderBlock(this decimal item, IEnumerable<OrderBlock> lOrderBlock, long mintime = 24 * 10)
        {
            try
            {
                if (!(lOrderBlock?.Any() ?? false))
                    return (false, null);

                var date = DateTime.Now;
                var top = lOrderBlock.Where(x => (x.Mode == (int)EOrderBlockMode.TopPinbar || x.Mode == (int)EOrderBlockMode.TopInsideBar)
                                                    && item >= x.Focus && item < x.SL && (date - x.Date).TotalHours >= mintime);
                if (top != null)
                    return (true, top);

                var bot = lOrderBlock.Where(x => (x.Mode == (int)EOrderBlockMode.BotPinbar || x.Mode == (int)EOrderBlockMode.BotPinbar)
                                                   && item <= x.Focus && item > x.SL && (date - x.Date).TotalHours >= mintime);

                if (bot != null)
                    return (true, bot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PartternService.IsOrderBlock|EXCEPTION| {ex.Message}");
            }
            return (false, null);
        }

        public static List<OrderBlock> GetOrderBlock(this List<Quote> lData, int minrate)
        {
            var lOrderBlock = new List<OrderBlock>();
            try
            {
                var lVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                });
                var lMa20Vol = lVol.GetSma(20);

                var lPivot = lData.GetTopBottom_H(minrate);
                var lBot = lPivot.Where(x => x.IsBot).ToList();
                lBot.Reverse();
                var lRemove = new List<TopBotModel>();
                TopBotModel itemCheck = null;
                foreach (var item in lBot)
                {
                    if (itemCheck is null)
                    {
                        itemCheck = item;
                        continue;
                    }

                    if (item.Value >= itemCheck.Value)
                    {
                        lRemove.Add(item);
                    }
                    else
                    {
                        itemCheck = item;
                    }
                }
                if (lRemove.Any())
                {
                    lBot = lBot.Except(lRemove).ToList();
                }
                //
                var lTop = lPivot.Where(x => x.IsTop).ToList();
                lTop.Reverse();
                itemCheck = null;
                lRemove.Clear();
                foreach (var item in lTop)
                {
                    if (itemCheck is null)
                    {
                        itemCheck = item;
                        continue;
                    }

                    if (item.Value <= itemCheck.Value)
                    {
                        lRemove.Add(item);
                    }
                    else
                    {
                        itemCheck = item;
                    }
                }
                if (lRemove.Any())
                {
                    lTop = lTop.Except(lRemove).ToList();
                }
                lPivot.Clear();
                lPivot.AddRange(lBot);
                lPivot.AddRange(lTop);
                if (!lPivot.Any())
                    return lOrderBlock;

                foreach (var pivot in lPivot.OrderBy(x => x.Date))
                {
                    var item = lData.First(x => x.Date == pivot.Date);
                    var volMa20 = lMa20Vol.First(x => x.Date == pivot.Date);
                    var len = item.High - item.Low;
                    var avgLen = lData.Where(x => x.Date <= pivot.Date).TakeLast(5).Average(x => x.High - x.Low);

                    var next = lData.FirstOrDefault(x => x.Date > pivot.Date);
                    var volMa20_Next = lMa20Vol.First(x => x.Date == next.Date);
                    var len_Next = next.High - next.Low;

                    var prev = lData.LastOrDefault(x => x.Date < pivot.Date);

                    if (pivot.IsTop)
                    {
                        var uplen = item.High - Math.Max(item.Open, item.Close);
                        if (uplen / len >= (decimal)0.5)
                        {
                            if (item.Volume >= (decimal)(volMa20.Sma.Value * 1.2) && len >= avgLen * (decimal)1.3)
                            {
                                var entry = item.High - uplen / 5;
                                var sl = entry + uplen / 2;
                                var tp = entry - uplen;
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
                                    TP = tp,
                                    Focus = Math.Max(item.Open, item.Close),
                                });
                                continue;
                                //Console.WriteLine($"TOP(pinbar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                            }
                        }

                        if (item.Volume >= (decimal)(volMa20.Sma.Value * 1.2) && len >= avgLen * (decimal)1.3)
                        {
                            if (item.Open <= item.Close || item.Close >= Math.Min(prev.Open, prev.Close))
                                continue;

                            var entry = prev.High - Math.Abs(prev.High - prev.Low) / 5;
                            var sl1 = entry + Math.Abs(prev.High - prev.Low) / 2;
                            var sl2 = item.High + Math.Abs(prev.High - prev.Low) / 5;
                            var sl = sl1 > sl2 ? sl1 : sl2;
                            var tp = entry - 2 * Math.Abs(sl - entry);
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
                                TP = tp,
                                Focus = Math.Min(item.Open, item.Close)
                            });
                            //Console.WriteLine($"TOP(outsidebar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                        }
                        else if (next.Volume >= (decimal)(volMa20_Next.Sma.Value * 1.2) && len_Next >= avgLen * (decimal)1.3)
                        {
                            if (next.Open <= next.Close || next.Close >= Math.Min(item.Open, item.Close))
                                continue;

                            var entry = item.High - Math.Abs(item.High - item.Low) / 5;
                            var sl1 = entry + Math.Abs(item.High - item.Low) / 2;
                            var sl2 = next.High + Math.Abs(item.High - item.Low) / 5;
                            var sl = sl1 > sl2 ? sl1 : sl2;
                            var tp = entry - 2 * Math.Abs(sl - entry);
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
                                TP = tp,
                                Focus = Math.Min(item.Open, item.Close)
                            });
                            //Console.WriteLine($"TOP(outsidebar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                        }
                    }
                    else
                    {
                        var belowLen = Math.Min(item.Open, item.Close) - item.Low;
                        if (belowLen / len >= (decimal)0.5)
                        {
                            if (item.Volume >= (decimal)(volMa20.Sma.Value * 1.2) && len >= avgLen * (decimal)1.3)
                            {
                                var entry = item.Low + belowLen / 5;
                                var sl = entry - belowLen / 2;
                                var tp = entry + belowLen;
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
                                    TP = tp,
                                    Focus = Math.Max(item.Open, item.Close)
                                });
                                continue;
                            }
                        }

                        if ((item.Volume < (decimal)(volMa20.Sma.Value * 1.2) || len < avgLen * (decimal)1.3))
                        {
                            if (item.Open >= item.Close || item.Close <= Math.Max(prev.Open, prev.Close))
                                continue;

                            var entry = prev.Low + Math.Abs(prev.High - prev.Low) / 5;
                            var sl1 = entry - Math.Abs(prev.High - prev.Low) / 2;
                            var sl2 = item.Low - Math.Abs(prev.High - prev.Low) / 5;
                            var sl = sl1 < sl2 ? sl1 : sl2;
                            var tp = entry + 2 * Math.Abs(sl - entry);
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
                                TP = tp,
                                Focus = Math.Min(item.Open, item.Close)
                            });
                            //Console.WriteLine($"BOT(outsidebar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
                        }
                        else if (next.Volume >= (decimal)(volMa20_Next.Sma.Value * 1.2) && len_Next >= avgLen * (decimal)1.3)
                        {
                            if (next.Open >= next.Close || next.Close <= Math.Max(item.Open, item.Close))
                                continue;

                            var entry = item.Low + Math.Abs(item.High - item.Low) / 5;
                            var sl1 = entry - Math.Abs(item.High - item.Low) / 2;
                            var sl2 = next.Low - Math.Abs(item.High - item.Low) / 5;
                            var sl = sl1 < sl2 ? sl1 : sl2;
                            var tp = entry + 2 * Math.Abs(sl - entry);
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
                                TP = tp,
                                Focus = Math.Min(item.Open, item.Close)
                            });
                            //Console.WriteLine($"BOT(outsidebar): {item.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {entry}|SL: {sl}");
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
                Console.WriteLine($"IndicatorExtention.GetTopBottom_HL|EXCEPTION| {ex.Message}");
            }
            return lResult;
        }
    }
}
