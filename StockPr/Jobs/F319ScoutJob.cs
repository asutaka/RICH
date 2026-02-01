using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class F319ScoutJob : IJob
    {
        private readonly ILogger<F319ScoutJob> _logger;
        private readonly IF319Service _f319Service;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public F319ScoutJob(
            ILogger<F319ScoutJob> logger,
            IF319Service f319Service,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _f319Service = f319Service;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job F319ScoutJob started.");
                var f319 = await _f319Service.F319KOL();
                if (f319.Any())
                {
                    await _teleService.SendMessages(_telegramSettings.GroupF319Id, f319.Select(x => x.Message), true);
                }
                _logger.LogInformation("Job F319ScoutJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "F319ScoutJob execution failed");
            }
        }
    }
}
