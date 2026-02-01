using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class ForexMorningJob : IJob
    {
        private readonly ILogger<ForexMorningJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public ForexMorningJob(
            ILogger<ForexMorningJob> logger,
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
                _logger.LogInformation("Job ForexMorningJob started.");
                var gdnn = await _analyzeService.ThongkeForeign_PhienSang(DateTime.Now);
                if (gdnn.Item1 > 0)
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, gdnn.Item2, true);
                }
                _logger.LogInformation("Job ForexMorningJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForexMorningJob execution failed");
            }
        }
    }
}
