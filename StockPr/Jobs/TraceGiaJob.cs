using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class TraceGiaJob : IJob
    {
        private readonly ILogger<TraceGiaJob> _logger;
        private readonly IGiaNganhHangService _giaService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public TraceGiaJob(
            ILogger<TraceGiaJob> logger,
            IGiaNganhHangService giaService,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _giaService = giaService;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job TraceGiaJob started.");
                
                // Get data from JobDataMap if we need to know if it's EndOfDay
                bool isEndOfDay = context.MergedJobDataMap.GetBoolean("IsEndOfDay");
                
                var res = await _giaService.TraceGia(isEndOfDay);
                if (res.Item1 > 0)
                {
                    await _teleService.SendMessage(_telegramSettings.GroupId, res.Item2, true);
                }
                
                _logger.LogInformation("Job TraceGiaJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TraceGiaJob execution failed");
            }
        }
    }
}
