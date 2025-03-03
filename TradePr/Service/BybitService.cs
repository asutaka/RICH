using Bybit.Net.Objects.Models.V5;
using TradePr.DAL;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitService
    {
        Task<BybitAssetBalance> GetAccountInfo();
        Task TradeTokenUnlock();
        Task TradeThreeSignal();
        Task MarketAction();
    }
    public class BybitService : IBybitService
    {
        private readonly ILogger<BybitService> _logger;
        private readonly ICacheService _cacheService;
        private readonly ITradingRepo _tradingRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly IThreeSignalTradeRepo _threeSignalTradeRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        public BybitService(ILogger<BybitService> logger, ICacheService cacheService,
                            ITradingRepo tradingRepo, IAPIService apiService, ITokenUnlockTradeRepo tokenUnlockTradeRepo,
                            IThreeSignalTradeRepo threeSignalTradeRepo, ITeleService teleService, IErrorPartnerRepo errRepo)
        {
            _logger = logger;
            _cacheService = cacheService;
            _tradingRepo = tradingRepo;
            _apiService = apiService;
            _teleService = teleService;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _threeSignalTradeRepo = threeSignalTradeRepo;
            _errRepo = errRepo;
        }
        public async Task<BybitAssetBalance> GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.ByBitInstance().V5Api.Account.GetBalancesAsync( Bybit.Net.Enums.AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public Task MarketAction()
        {
            throw new NotImplementedException();
        }

        public Task TradeThreeSignal()
        {
            throw new NotImplementedException();
        }

        public Task TradeTokenUnlock()
        {
            throw new NotImplementedException();
        }
    }
}
