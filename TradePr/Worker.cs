using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly IBybitWyckoffService _bybitWyckoffService;
        private readonly ITakerService _takerService;

        public Worker(IBybitWyckoffService bybitWyckoffService, ITakerService takerService)
        {
            _bybitWyckoffService = bybitWyckoffService;
            _takerService = takerService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _mmService.Bybit_GetAccountInfo();

            while (!stoppingToken.IsCancellationRequested)
            {
                await _takerService.Bybit_Trade();
                await _takerService.Bybit_Signal();
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
