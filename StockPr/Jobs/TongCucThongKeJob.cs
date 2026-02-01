using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class TongCucThongKeJob : IJob
    {
        private readonly ILogger<TongCucThongKeJob> _logger;
        private readonly ITongCucThongKeService _tongcucService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public TongCucThongKeJob(
            ILogger<TongCucThongKeJob> logger,
            ITongCucThongKeService tongcucService,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _tongcucService = tongcucService;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job TongCucThongKeJob started.");
                var res = await _tongcucService.TongCucThongKe(DateTime.Now);
                if (res.Item1 > 0)
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, res.Item2);
                }
                _logger.LogInformation("Job TongCucThongKeJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TongCucThongKeJob execution failed");
            }
        }
    }
}
