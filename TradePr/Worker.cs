using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly IBybitWyckoffService _bybitWyckoffService;

        public Worker(IBybitWyckoffService bybitWyckoffService)
        {
            _bybitWyckoffService = bybitWyckoffService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _bybitService.Bybit_GetAccountInfo();

            while (!stoppingToken.IsCancellationRequested)
            {
                await _bybitWyckoffService.Bybit_Trade();
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
