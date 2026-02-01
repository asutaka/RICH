using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class BaoCaoPhanTichJob : IJob
    {
        private readonly ILogger<BaoCaoPhanTichJob> _logger;
        private readonly IBaoCaoPhanTichService _bcptService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public BaoCaoPhanTichJob(
            ILogger<BaoCaoPhanTichJob> logger,
            IBaoCaoPhanTichService bcptService,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _bcptService = bcptService;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job BaoCaoPhanTichJob started.");
                var lBCPT = await _bcptService.BaoCaoPhanTich();
                
                if (lBCPT.Item1?.Any() ?? false)
                {
                    foreach (var item in lBCPT.Item1)
                    {
                        await _teleService.SendMessage(_telegramSettings.GroupId, item.content, item.link);
                        await Task.Delay(200);
                    }
                }

                if (lBCPT.Item2?.Any() ?? false)
                {
                    var mes = string.Join("\n", lBCPT.Item2);
                    await _teleService.SendMessage(_telegramSettings.UserId, mes);
                }
                
                _logger.LogInformation("Job BaoCaoPhanTichJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BaoCaoPhanTichJob execution failed");
            }
        }
    }
}
