using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class MorningSetupJob : IJob
    {
        private readonly ILogger<MorningSetupJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public MorningSetupJob(
            ILogger<MorningSetupJob> logger,
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
                _logger.LogInformation("Job MorningSetupJob started.");
                var mes = await _analyzeService.DetectEntry();
                if (!string.IsNullOrWhiteSpace(mes))
                {
                    await _teleService.SendMessage(_telegramSettings.UserId, mes, true);
                }
                _logger.LogInformation("Job MorningSetupJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MorningSetupJob execution failed");
            }
        }
    }
}
