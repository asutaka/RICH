using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly IBybitWyckoffService _bybitWyckoffService;
        private readonly IMMService _mmService;

        public Worker(IBybitWyckoffService bybitWyckoffService, IMMService mmService)
        {
            _bybitWyckoffService = bybitWyckoffService;
            _mmService = mmService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _mmService.Bybit_GetAccountInfo();

            while (!stoppingToken.IsCancellationRequested)
            {
                await _mmService.Bybit_Trade();
                await _mmService.Bybit_Signal();
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
