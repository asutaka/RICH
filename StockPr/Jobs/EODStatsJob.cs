using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class EODStatsJob : IJob
    {
        private readonly ILogger<EODStatsJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public EODStatsJob(
            ILogger<EODStatsJob> logger,
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
                _logger.LogInformation("Job EODStatsJob started.");
                
                // 1. Thống kê GDNN
                var gdnnMsg = await _analyzeService.ThongKeGDNN_NhomNganh();
                if (!string.IsNullOrWhiteSpace(gdnnMsg))
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, gdnnMsg, true);
                }

                // 2. Chỉ báo kỹ thuật
                var cbkt = await _analyzeService.ChiBaoKyThuat(DateTime.Now, true);
                if (cbkt.Item1 > 0)
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, cbkt.Item2, true);
                }

                // 3. Phân tích chỉ số ngành
                var sectorMsg = await _analyzeService.AnalyzeSectorIndex();
                if (sectorMsg.Item1 > 0)
                {
                    await _teleService.SendMessage(_telegramSettings.UserId, sectorMsg.Item2, true);
                }

                _logger.LogInformation("Job EODStatsJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EODStatsJob execution failed");
            }
        }
    }
}
