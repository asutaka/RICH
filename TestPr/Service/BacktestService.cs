using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.Model;
using Skender.Stock.Indicators;
using TestPr.Model;

namespace TestPr.Service
{
    public interface IBacktestService
    {
        Task<BacktestResult> RunBacktest(BacktestConfig config);
    }

    public class BacktestService : IBacktestService
    {
        private readonly ILogger<BacktestService> _logger;
        private readonly IAPIService _apiService;

        public BacktestService(ILogger<BacktestService> logger, IAPIService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        public async Task<BacktestResult> RunBacktest(BacktestConfig config)
        {
            var result = new BacktestResult
            {
                StartDate = config.StartDate,
                EndDate = config.EndDate
            };

            Console.WriteLine($"=== BẮT ĐẦU BACKTEST ===");
            Console.WriteLine($"Thời gian: {config.StartDate:dd/MM/yyyy} -> {config.EndDate:dd/MM/yyyy}");
            Console.WriteLine($"Số coin: {config.Symbols.Count}");
            Console.WriteLine($"Danh sách: {string.Join(", ", config.Symbols)}");
            Console.WriteLine($"========================\n");

            foreach (var symbol in config.Symbols)
            {
                try
                {
                    Console.WriteLine($"[{symbol}] Đang tải dữ liệu và backtest...");
                    await BacktestSymbol(symbol, config, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi backtest {symbol}: {ex.Message}");
                    Console.WriteLine($"[{symbol}] LỖI: {ex.Message}");
                }
            }

            // Tính toán kết quả tổng hợp
            CalculateResults(result);
            PrintResults(result);

            return result;
        }

        private async Task BacktestSymbol(string symbol, BacktestConfig config, BacktestResult result)
        {
            // Lấy dữ liệu lịch sử từ Binance
            var quotes = await _apiService.GetData_Binance(symbol, EInterval.M15);
            if (quotes == null || !quotes.Any())
            {
                Console.WriteLine($"[{symbol}] Không có dữ liệu");
                return;
            }

            // Lọc theo khoảng thời gian
            quotes = quotes.Where(q => q.Date >= config.StartDate && q.Date <= config.EndDate).ToList();
            if (quotes.Count < 50)
            {
                Console.WriteLine($"[{symbol}] Không đủ dữ liệu (chỉ có {quotes.Count} nến)");
                return;
            }

            // Lấy volume data
            var takevolumes = await _apiService.GetBuySellRate(symbol, EInterval.M15, 500);

            Trade? openTrade = null;
            TakerVolumneBuySellDTO? signal = null;

            // Duyệt qua từng nến
            for (int i = 50; i < quotes.Count; i++)
            {
                var currentQuotes = quotes.Take(i + 1).ToList();
                var last = currentQuotes.Last();
                var timestamp = (decimal)new DateTimeOffset(last.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
                var currentTakeVolume = takevolumes?.Where(tv => tv.timestamp <= timestamp).ToList();
                if (!currentTakeVolume.Any())
                    continue;

                // Nếu đang có lệnh mở -> kiểm tra TP/SL
                if (openTrade != null)
                {
                    var exitInfo = CheckTakeProfit(openTrade, currentQuotes, config);
                    if (exitInfo != null)
                    {
                        openTrade.Exits.Add(exitInfo);
                        openTrade.RemainingQuantity -= exitInfo.Quantity;

                        if (openTrade.IsClosed)
                        {
                            result.Trades.Add(openTrade);
                            openTrade = null;
                            signal = null; // Reset signal
                        }
                    }
                    continue;
                }

                // Kiểm tra tín hiệu entry
                if (signal != null)
                {
                    // Đã có signal, kiểm tra điểm entry

                    var entry = currentQuotes.GetEntry();
                    if (entry.Item1 == 0)
                    {
                        signal = null; // Signal không còn hợp lệ
                    }
                    else if (entry.Item1 > 0)
                    {
                        // Entry thành công!
                        var sl_price = Math.Min(last.Open, last.Close) * (1 - config.StopLossRate / 100);
                        openTrade = new Trade
                        {
                            Symbol = symbol,
                            EntryDate = last.Date,
                            EntryPrice = last.Close,
                            EntryQuote = entry.Item2,
                            StopLoss = sl_price,
                            RemainingQuantity = 100
                        };
                        signal = null;
                    }
                }
                else
                {
                    // Tìm signal mới
                    if (currentTakeVolume != null && currentTakeVolume.Any())
                    {
                        signal = currentTakeVolume.GetSignal(currentQuotes);
                    }
                }
            }

            // Đóng các lệnh còn mở khi hết data
            if (openTrade != null)
            {
                var lastQuote = quotes.Last();
                openTrade.Exits.Add(new Exit
                {
                    ExitDate = lastQuote.Date,
                    ExitPrice = lastQuote.Close,
                    Quantity = openTrade.RemainingQuantity,
                    Reason = "END_OF_DATA",
                    ProfitLoss = (lastQuote.Close - openTrade.EntryPrice) / openTrade.EntryPrice * 100
                });
                openTrade.RemainingQuantity = 0;
                result.Trades.Add(openTrade);
            }

            Console.WriteLine($"[{symbol}] Hoàn thành - Có {result.Trades.Count(t => t.Symbol == symbol)} giao dịch");
        }

        private Exit? CheckTakeProfit(Trade trade, List<Quote> quotes, BacktestConfig config)
        {
            var current = quotes[^2]; // Nến trước nến cuối
            var last = quotes.Last();

            // Kiểm tra số nến đã giữ
            var candlesHeld = quotes.Count(q => q.Date > trade.EntryDate);
            if (candlesHeld > config.MaxCandlesHold)
            {
                return new Exit
                {
                    ExitDate = last.Date,
                    ExitPrice = last.Close,
                    Quantity = trade.RemainingQuantity,
                    Reason = "TIMEOUT",
                    ProfitLoss = (last.Close - trade.EntryPrice) / trade.EntryPrice * 100 * (trade.RemainingQuantity / 100)
                };
            }

            // Kiểm tra STOP LOSS
            if (last.Low <= trade.StopLoss)
            {
                return new Exit
                {
                    ExitDate = last.Date,
                    ExitPrice = trade.StopLoss,
                    Quantity = trade.RemainingQuantity,
                    Reason = "SL",
                    ProfitLoss = (trade.StopLoss - trade.EntryPrice) / trade.EntryPrice * 100 * (trade.RemainingQuantity / 100)
                };
            }

            // Tính Bollinger Bands và RSI
            var lbb = quotes.GetBollingerBands().ToList();
            var lrsi = quotes.GetRsi().ToList();
            var lma9 = lrsi.GetSma(9).ToList();
            var bb_cur = lbb[^2];

            // TP1 – bán 40%
            if (trade.RemainingQuantity == 100 && 
                current.Close > trade.EntryQuote.Close &&
                current.Close >= (decimal)bb_cur.Sma.Value)
            {
                return new Exit
                {
                    ExitDate = current.Date,
                    ExitPrice = current.Close,
                    Quantity = 40,
                    Reason = "TP1",
                    ProfitLoss = (current.Close - trade.EntryPrice) / trade.EntryPrice * 100 * 0.4m
                };
            }

            // TP2 – bán 40%
            if (trade.RemainingQuantity == 60 &&
                current.High > (decimal)bb_cur.UpperBand.Value)
            {
                return new Exit
                {
                    ExitDate = current.Date,
                    ExitPrice = current.Close,
                    Quantity = 40,
                    Reason = "TP2",
                    ProfitLoss = (current.Close - trade.EntryPrice) / trade.EntryPrice * 100 * 0.4m
                };
            }

            // TP3 – bán toàn bộ
            if (trade.RemainingQuantity <= 60 && // Đã TP1
                lrsi[^2].Rsi < lma9[^2].Sma)
            {
                return new Exit
                {
                    ExitDate = current.Date,
                    ExitPrice = current.Close,
                    Quantity = trade.RemainingQuantity,
                    Reason = "TP3",
                    ProfitLoss = (current.Close - trade.EntryPrice) / trade.EntryPrice * 100 * (trade.RemainingQuantity / 100)
                };
            }

            return null;
        }

        private void CalculateResults(BacktestResult result)
        {
            result.TotalTrades = result.Trades.Count;
            result.WinningTrades = result.Trades.Count(t => t.TotalProfitLoss > 0);
            result.LosingTrades = result.Trades.Count(t => t.TotalProfitLoss <= 0);
            result.TotalProfitLoss = result.Trades.Sum(t => t.TotalProfitLoss);
            result.BestTrade = result.Trades.Any() ? result.Trades.Max(t => t.TotalProfitLoss) : 0;
            result.WorstTrade = result.Trades.Any() ? result.Trades.Min(t => t.TotalProfitLoss) : 0;

            // Thống kê theo symbol
            foreach (var trade in result.Trades)
            {
                if (!result.TradesBySymbol.ContainsKey(trade.Symbol))
                {
                    result.TradesBySymbol[trade.Symbol] = 0;
                    result.ProfitBySymbol[trade.Symbol] = 0;
                }
                result.TradesBySymbol[trade.Symbol]++;
                result.ProfitBySymbol[trade.Symbol] += trade.TotalProfitLoss;
            }
        }

        private void PrintResults(BacktestResult result)
        {
            Console.WriteLine("\n================================");
            Console.WriteLine("      KẾT QUẢ BACKTEST");
            Console.WriteLine("================================");
            Console.WriteLine($"Tổng số giao dịch: {result.TotalTrades}");
            Console.WriteLine($"Giao dịch thắng: {result.WinningTrades} ({result.WinRate:F2}%)");
            Console.WriteLine($"Giao dịch thua: {result.LosingTrades}");
            Console.WriteLine($"--------------------------------");
            Console.WriteLine($"Tổng P/L: {result.TotalProfitLoss:F2}%");
            Console.WriteLine($"P/L trung bình: {result.AverageProfitLoss:F2}%");
            Console.WriteLine($"Giao dịch tốt nhất: {result.BestTrade:F2}%");
            Console.WriteLine($"Giao dịch tệ nhất: {result.WorstTrade:F2}%");
            
            Console.WriteLine("\n--- Thống kê theo coin ---");
            foreach (var kvp in result.TradesBySymbol.OrderByDescending(x => result.ProfitBySymbol[x.Key]))
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value} giao dịch, P/L = {result.ProfitBySymbol[kvp.Key]:F2}%");
            }

            Console.WriteLine("\n--- Chi tiết các giao dịch ---");
            foreach (var trade in result.Trades.OrderBy(t => t.EntryDate))
            {
                Console.WriteLine($"\n[{trade.Symbol}] Entry: {trade.EntryDate:dd/MM HH:mm} @ {trade.EntryPrice:F4}");
                foreach (var exit in trade.Exits)
                {
                    Console.WriteLine($"  → {exit.Reason}: {exit.ExitDate:dd/MM HH:mm} @ {exit.ExitPrice:F4} ({exit.Quantity}%) | P/L: {exit.ProfitLoss:F2}%");
                }
                Console.WriteLine($"  TỔNG P/L: {trade.TotalProfitLoss:F2}%");
            }
            Console.WriteLine("================================\n");
        }
    }
}
