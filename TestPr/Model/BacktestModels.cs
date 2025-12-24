using Skender.Stock.Indicators;

namespace TestPr.Model
{
    /// <summary>
    /// Thông tin một giao dịch trong backtest
    /// </summary>
    public class Trade
    {
        public string Symbol { get; set; }
        public DateTime EntryDate { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal StopLoss { get; set; }
        public Quote EntryQuote { get; set; } // Lưu quote entry để tính TP
        public decimal RemainingQuantity { get; set; } = 100; // Bắt đầu 100%, giảm dần khi TP1/TP2
        public List<Exit> Exits { get; set; } = new List<Exit>();
        public bool IsClosed => RemainingQuantity <= 0;
        
        /// <summary>
        /// Tính tổng P/L của toàn bộ giao dịch
        /// </summary>
        public decimal TotalProfitLoss => Exits.Sum(e => e.ProfitLoss);
    }

    /// <summary>
    /// Thông tin một lần chốt lời/lỗ
    /// </summary>
    public class Exit
    {
        public DateTime ExitDate { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal Quantity { get; set; } // % của vị thế ban đầu
        public string Reason { get; set; } // "TP1", "TP2", "TP3", "SL", "TIMEOUT"
        public decimal ProfitLoss { get; set; } // % lợi nhuận/lỗ
    }

    /// <summary>
    /// Kết quả backtest tổng thể
    /// </summary>
    public class BacktestResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100 : 0;
        public decimal TotalProfitLoss { get; set; }
        public decimal AverageProfitLoss => TotalTrades > 0 ? TotalProfitLoss / TotalTrades : 0;
        public decimal BestTrade { get; set; }
        public decimal WorstTrade { get; set; }
        public Dictionary<string, int> TradesBySymbol { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> ProfitBySymbol { get; set; } = new Dictionary<string, decimal>();
        public List<Trade> Trades { get; set; } = new List<Trade>();
    }

    /// <summary>
    /// Cấu hình cho backtest
    /// </summary>
    public class BacktestConfig
    {
        public List<string> Symbols { get; set; } = new List<string>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxCandlesHold { get; set; } = 24; // SONEN_NAMGIU
        public decimal StopLossRate { get; set; } = 1.5m; // SL_RATE
    }
}
