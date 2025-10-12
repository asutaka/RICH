using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly IBybitWyckoffService _bybitWyckoffService;
        private readonly ILast_BybitService _lastService;

        public Worker(IBybitWyckoffService bybitWyckoffService, ILast_BybitService lastService)
        {
            _bybitWyckoffService = bybitWyckoffService;
            _lastService = lastService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _bybitWyckoffService.Bybit_GetAccountInfo();

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    await _bybitWyckoffService.Bybit_TakeProfit_1809();

            //    var now = DateTime.Now;
            //    if(now.Minute == 30)
            //    {
            //        _bybitWyckoffService.Bybit_Signal_1809();
            //    }    
            //    if(now.Minute == 0)
            //    {
            //        await _bybitWyckoffService.Bybit_Entry_1809();
            //    }
                
            //    await Task.Delay(1000 * 60, stoppingToken);
            //}
            //


            while (!stoppingToken.IsCancellationRequested)
            {
                await _lastService.Detect_TakeProfit();

                var now = DateTime.Now;
                if (now.Minute == 30)
                {
                    _lastService.Detect_SOSReal();
                }
                if (now.Minute == 0)
                {
                    await _lastService.Detect_Entry();
                }

                if (now.Hour % 4 == 0
                    && now.Minute <= 1)
                {
                    await _lastService.Detect_SOS();
                }

                await Task.Delay(1000 * 60, stoppingToken);
            }
            //
        }
    }
}
