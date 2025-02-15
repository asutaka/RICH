using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface ICacheService
    {
        IEnumerable<Trading> GetListTrading();
        IEnumerable<TokenUnlock> GetTokenUnlock(DateTime dt);
    }
    public class CacheService : ICacheService
    {
        private readonly ILogger<CacheService> _logger;
        private readonly IMemoryCache _cache;
        private readonly ITradingRepo _tradingRepo;
        private readonly ITokenUnlockRepo _tokenUnlockRepo;
        public CacheService(ILogger<CacheService> logger, IMemoryCache memoryCache, ITradingRepo tradingRepo, ITokenUnlockRepo tokenUnlockRepo)
        {
            _logger = logger;
            _cache = memoryCache;
            _tradingRepo = tradingRepo;
            _tokenUnlockRepo = tokenUnlockRepo;
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

        public IEnumerable<TokenUnlock> GetTokenUnlock(DateTime dt)
        {
            var key = "lTokenUnlockCache";
            var lCache = _cache.Get<IEnumerable<TokenUnlock>>(key);
            var time = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1).ToUnixTimeSeconds();
            var timeNext = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).AddDays(2).ToUnixTimeSeconds();
            try
            {
                //if (lCache?.Any() != null)
                //    return lCache;

                lCache = _tokenUnlockRepo.GetByFilter(Builders<TokenUnlock>.Filter.Gte(x => x.time, time)).Where(x => x.time < timeNext);
                return lCache;
                //lCache = _tokenUnlockRepo.GetByFilter(Builders<TokenUnlock>.Filter.Gte(x => x.time, time)).Where(x => x.time < timeNext
                //                                                                                                   && !StaticVal._lTokenUnlockBlackList.Contains(x.s));
                _cache.Set(key, lCache, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CacheService.GetTokenUnlock|EXCEPTION| {ex.Message}");
            }

            return lCache;
        }
    }
}
