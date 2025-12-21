using ChartVisualizationPr.Models;
using Skender.Stock.Indicators;
using StockPr.Service;
using StockPr.DAL;
using MongoDB.Driver;

namespace ChartVisualizationPr.Services
{
    public interface IChartDataService
    {
        Task<List<string>> GetSymbolsAsync();
        Task<List<CandleData>> GetCandleDataAsync(string symbol);
        Task<List<IndicatorData>> GetIndicatorDataAsync(string symbol);
        Task<List<MarkerData>> GetMarkersAsync(string symbol);
        Task<MarkerData> SaveMarkerAsync(MarkerData marker);
        Task<bool> DeleteMarkerAsync(string id);
        Task<List<InvestorData>> GetGroupDataAsync(string symbol);
        Task<List<InvestorData>> GetForeignDataAsync(string symbol);
        Task<List<InvestorData>> GetNetTradeVolumeDataAsync(string symbol);
    }

    public class ChartDataService : IChartDataService
    {
        private readonly IAPIService _apiService;
        private readonly ILogger<ChartDataService> _logger;
        private readonly IPhanLoaiNDTRepo _phanLoaiNDTRepo;
        private readonly IStockRepo _stockRepo;
        private static readonly List<MarkerData> _markers = new(); // In-memory storage for demo

        public ChartDataService(IAPIService apiService, ILogger<ChartDataService> logger, IPhanLoaiNDTRepo phanLoaiNDTRepo, IStockRepo stockRepo)
        {
            _apiService = apiService;
            _logger = logger;
            _phanLoaiNDTRepo = phanLoaiNDTRepo;
            _stockRepo = stockRepo;
        }

        public async Task<List<string>> GetSymbolsAsync()
        {
            try
            {
                var stocks = await Task.Run(() => _stockRepo.GetAll());
                return stocks?.Select(s => s.s)
                             .Where(s => !string.IsNullOrEmpty(s))
                             .OrderBy(s => s)
                             .ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symbols");
                throw;
            }
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
                        Close = (decimal)r.Rsi!.Value
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

        public async Task<List<InvestorData>> GetGroupDataAsync(string symbol)
        {
            try
            {
                // Query PhanLoaiNDT by symbol using FilterDefinition
                var filter = Builders<StockPr.DAL.Entity.PhanLoaiNDT>.Filter.Eq(x => x.s, symbol);
                var phanLoaiData = await Task.Run(() => _phanLoaiNDTRepo.GetByFilter(filter));
                var data = phanLoaiData.FirstOrDefault();

                if (data == null || data.Date == null || data.Group == null)
                {
                    return new List<InvestorData>();
                }

                // Map Date[i] to Group[i]
                var result = new List<InvestorData>();
                for (int i = 0; i < Math.Min(data.Date.Count, data.Group.Count); i++)
                {
                    result.Add(new InvestorData
                    {
                        time = (long)data.Date[i],
                        value = data.Group[i]
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting group data for {symbol}");
                throw;
            }
        }

        public async Task<List<InvestorData>> GetForeignDataAsync(string symbol)
        {
            try
            {
                var filter = Builders<StockPr.DAL.Entity.PhanLoaiNDT>.Filter.Eq(x => x.s, symbol);
                var phanLoaiData = await Task.Run(() => _phanLoaiNDTRepo.GetByFilter(filter));
                var data = phanLoaiData.FirstOrDefault();

                if (data == null || data.Date == null || data.Foreign == null)
                {
                    return new List<InvestorData>();
                }

                var result = new List<InvestorData>();
                for (int i = 0; i < Math.Min(data.Date.Count, data.Foreign.Count); i++)
                {
                    result.Add(new InvestorData
                    {
                        time = (long)data.Date[i],
                        value = data.Foreign[i]
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting foreign data for {symbol}");
                throw;
            }
        }

        public async Task<List<InvestorData>> GetNetTradeVolumeDataAsync(string symbol)
        {
            try
            {
                var now = DateTime.Now;
                var from = now.AddYears(-1);

                var data = await _apiService.SSI_GetStockInfo(symbol, from, now);

                if (data?.data == null)
                    return new List<InvestorData>();

                var result = data.data
                    .Where(d => !string.IsNullOrEmpty(d.tradingDate))
                    .Select(d =>
                    {
                        var date = DateTime.ParseExact(d.tradingDate, "dd/MM/yyyy", null);
                        return new InvestorData
                        {
                            time = new DateTimeOffset(date).ToUnixTimeSeconds(),
                            value = d.netTotalTradeVol
                        };
                    })
                    .OrderBy(d => d.time)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting net trade volume data for {symbol}");
                throw;
            }
        }
    }
}
