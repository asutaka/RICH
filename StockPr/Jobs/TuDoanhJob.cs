using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class TuDoanhJob : IJob
    {
        private readonly ILogger<TuDoanhJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public TuDoanhJob(
            ILogger<TuDoanhJob> logger,
            IAnalyzeService analyzeService,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _analyzeService = analyzeService;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job TuDoanhJob started.");
                var mes = await _analyzeService.ThongKeTuDoanh();
                if (!string.IsNullOrWhiteSpace(mes))
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, mes, true);
                }
                _logger.LogInformation("Job TuDoanhJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TuDoanhJob execution failed");
            }
        }
    }
}
