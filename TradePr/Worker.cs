using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly IBybitService _bybitService;

        public Worker(IBybitService bybitService)
        {
            _bybitService = bybitService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _bybitService.Bybit_GetAccountInfo();

            while (!stoppingToken.IsCancellationRequested)
            {
                await _bybitService.Bybit_Trade();
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
