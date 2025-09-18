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
                await _bybitWyckoffService.Bybit_TakeProfit_1809();

                var now = DateTime.Now;
                if(now.Minute == 30)
                {
                    _bybitWyckoffService.Bybit_Signal_1809();
                }    
                if(now.Minute == 0)
                {
                    await _bybitWyckoffService.Bybit_Entry_1809();
                }
                
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
