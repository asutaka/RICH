using Skender.Stock.Indicators;
using System.Xml.Linq;
using TestPr.Utils;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task MethodTest();
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        public TestService(ILogger<TestService> logger)
        {
            _logger = logger;
        }

        public async Task MethodTest()
        {
            try
            {
                var lBinance = await StaticVal.BinanceInstance().SpotApi.ExchangeData.GetKlinesAsync("BTCUSDT", Binance.Net.Enums.KlineInterval.OneDay, limit: 1000);
                var lDataBinance = lBinance.Data.Select(x => new Quote
                {
                    Date = x.OpenTime,
                    Open = x.OpenPrice,
                    High = x.HighPrice,
                    Low = x.LowPrice,
                    Close = x.ClosePrice,
                    Volume = x.Volume,
                }).ToList();
                var lOrderBlock = lDataBinance.GetOrderBlock(10);
                var tmp = 1;
                //var lBinance = await StaticVal.BinanceInstance().UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, BinanceInterval, limit: 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
        }
    }
}
