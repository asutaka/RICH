using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using TradePr.DAL;
using TradePr.DAL.Entity;

namespace TradePr.Service
{
    public interface ICacheService
    {
        IEnumerable<Trading> GetListTrading();
    }
    public class CacheService : ICacheService
    {
        private readonly ILogger<CacheService> _logger;
        private readonly IMemoryCache _cache;
        private readonly ITradingRepo _tradingRepo;
        public CacheService(ILogger<CacheService> logger, IMemoryCache memoryCache, ITradingRepo tradingRepo)
        {
            _logger = logger;
            _cache = memoryCache;
            _tradingRepo = tradingRepo;
        }

        public IEnumerable<Trading> GetListTrading()
        {
            var key = "lTokenCache";
            var lCache = _cache.Get<IEnumerable<Trading>>(key);
            var dt = (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds();
            try
            {
                if (lCache?.Any() != null)
                    return lCache;

                lCache = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, dt));
                _cache.Set(key, lCache, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CacheService.GetListTrading|EXCEPTION| {ex.Message}");
            }

            return lCache;
        }
    }
}
