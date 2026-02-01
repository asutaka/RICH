using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class AnalysisRealtimeJob : IJob
    {
        private readonly ILogger<AnalysisRealtimeJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public AnalysisRealtimeJob(
            ILogger<AnalysisRealtimeJob> logger,
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Job AnalysisRealtimeJob started.");
                var mes = await _analyzeService.Realtime();
                if (!string.IsNullOrWhiteSpace(mes))
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, mes, true);
                }
                sw.Stop();
                _logger.LogInformation("Job AnalysisRealtimeJob completed in {Duration}ms.", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "AnalysisRealtimeJob execution failed after {Duration}ms.", sw.ElapsedMilliseconds);
            }
        }
    }
}
