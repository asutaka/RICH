using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task GetAccountInfo();
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly string _api_key = string.Empty;
        private readonly string _api_secret = string.Empty;
        public BinanceService(ILogger<BinanceService> logger, IConfiguration config) 
        { 
            _logger = logger;
            _api_key = config["Account:API_KEY"];
            _api_secret = config["Account:SECRET_KEY"];
        }

        public async Task GetAccountInfo()
        {
            try
            {
                var tmp = await StaticVal.BinanceInstance(_api_key, _api_secret).SpotApi.Account.GetAccountInfoAsync();
                var tmp2 = await StaticVal.BinanceInstance(_api_key, _api_secret).UsdFuturesApi.Account.GetBalancesAsync();
                var tmp1 = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
        }
    }
}
