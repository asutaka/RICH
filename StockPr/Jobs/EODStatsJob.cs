using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;
using StockPr.Utils;

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

                // 4. Heatmap biến động ngành (VietStock GICS)
                var heatmapStream = await _analyzeService.Chart_Heatmap();
                if (heatmapStream != null)
                {
                    await _teleService.SendPhoto(_telegramSettings.UserId, heatmapStream);
                    var sectorMsgMap = string.Join("\n", StaticVal._dicSectorChartIndex
                        .Select(s => new { Sector = s.Key, Desc = StaticVal._dicSectorDescription.FirstOrDefault(d => d.Value == s.Value).Key })
                        .Where(x => !string.IsNullOrEmpty(x.Desc))
                        .Select(x => $"{x.Sector} : {x.Desc}"));
                    if (!string.IsNullOrEmpty(sectorMsgMap))
                    {
                        await _teleService.SendMessage(_telegramSettings.UserId, sectorMsgMap, true);
                    }
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
