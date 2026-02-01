using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class ChartStatsJob : IJob
    {
        private readonly ILogger<ChartStatsJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public ChartStatsJob(
            ILogger<ChartStatsJob> logger,
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
                _logger.LogInformation("Job ChartStatsJob started.");
                var stream = await _analyzeService.Chart_ThongKeKhopLenh();
                if (stream != null)
                {
                    await _teleService.SendPhoto(_telegramSettings.ChannelId, stream);
                }
                _logger.LogInformation("Job ChartStatsJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChartStatsJob execution failed");
            }
        }
    }
}
