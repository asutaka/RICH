using ChartVisualizationPr.Models;
using Skender.Stock.Indicators;
using StockPr.Service;

namespace ChartVisualizationPr.Services
{
    public interface IChartDataService
    {
        Task<List<CandleData>> GetCandleDataAsync(string symbol);
        Task<List<IndicatorData>> GetIndicatorDataAsync(string symbol);
        Task<List<MarkerData>> GetMarkersAsync(string symbol);
        Task<MarkerData> SaveMarkerAsync(MarkerData marker);
        Task<bool> DeleteMarkerAsync(string id);
    }

    public class ChartDataService : IChartDataService
    {
        private readonly IAPIService _apiService;
        private readonly ILogger<ChartDataService> _logger;
        private static readonly List<MarkerData> _markers = new(); // In-memory storage for demo

        public ChartDataService(IAPIService apiService, ILogger<ChartDataService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<List<CandleData>> GetCandleDataAsync(string symbol)
        {
            try
            {
                var quotes = await _apiService.SSI_GetDataStock(symbol);
                
                return quotes.Select(q => new CandleData
                {
                    Time = new DateTimeOffset(q.Date).ToUnixTimeSeconds(),
                    Open = q.Open,
                    High = q.High,
                    Low = q.Low,
                    Close = q.Close,
                    Volume = q.Volume
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting candle data for {symbol}");
                throw;
            }
        }

        public async Task<List<IndicatorData>> GetIndicatorDataAsync(string symbol)
        {
            try
            {
                var quotes = await _apiService.SSI_GetDataStock(symbol);
                
                // Calculate indicators
                var bbResults = quotes.GetBollingerBands().ToList();
                var rsiResults = quotes.GetRsi().ToList();


                // Convert RSI to Quote format for MA calculations
                var rsiQuotes = rsiResults
                    .Where(r => r.Rsi.HasValue)
                    .Select(r => new Quote
                    {
                        Date = r.Date,
                        Close = (decimal)r.Rsi.Value
                    })
                    .ToList();

                // Calculate MA9 and WMA45 from RSI (only if enough data)
                var rsiMa9Results = rsiQuotes.Count >= 9 ? rsiQuotes.GetSma(9).ToList() : new List<SmaResult>();
                var rsiWma45Results = rsiQuotes.Count >= 45 ? rsiQuotes.GetWma(45).ToList() : new List<WmaResult>();

                var indicators = new List<IndicatorData>();

                for (int i = 0; i < quotes.Count; i++)
                {
                    var quote = quotes[i];
                    var bb = bbResults.FirstOrDefault(x => x.Date == quote.Date);
                    var rsi = rsiResults.FirstOrDefault(x => x.Date == quote.Date);
                    var rsiMa9 = rsiMa9Results.FirstOrDefault(x => x.Date == quote.Date);
                    var rsiWma45 = rsiWma45Results.FirstOrDefault(x => x.Date == quote.Date);

                    indicators.Add(new IndicatorData
                    {
                        Time = new DateTimeOffset(quote.Date).ToUnixTimeSeconds(),
                        Ma20 = bb?.Sma != null ? (decimal)bb.Sma.Value : null,
                        UpperBand = bb?.UpperBand != null ? (decimal)bb.UpperBand.Value : null,
                        LowerBand = bb?.LowerBand != null ? (decimal)bb.LowerBand.Value : null,
                        Rsi = rsi?.Rsi != null ? (decimal)rsi.Rsi.Value : null,
                        RsiMa9 = rsiMa9?.Sma != null ? (decimal)rsiMa9.Sma.Value : null,
                        RsiWma45 = rsiWma45?.Wma != null ? (decimal)rsiWma45.Wma.Value : null,
                        Volume = quote.Volume
                    });
                }

                return indicators;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting indicator data for {symbol}");
                throw;
            }
        }

        public Task<List<MarkerData>> GetMarkersAsync(string symbol)
        {
            var markers = _markers.Where(m => m.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult(markers);
        }

        public Task<MarkerData> SaveMarkerAsync(MarkerData marker)
        {
            // Remove existing marker with same ID if exists
            _markers.RemoveAll(m => m.Id == marker.Id);
            _markers.Add(marker);
            return Task.FromResult(marker);
        }

        public Task<bool> DeleteMarkerAsync(string id)
        {
            var removed = _markers.RemoveAll(m => m.Id == id);
            return Task.FromResult(removed > 0);
        }
    }
}
