using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class PortfolioJob : IJob
    {
        private readonly ILogger<PortfolioJob> _logger;
        private readonly IPortfolioService _portfolioService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public PortfolioJob(
            ILogger<PortfolioJob> logger,
            IPortfolioService portfolioService,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _portfolioService = portfolioService;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job PortfolioJob started.");
                var portfolio = await _portfolioService.Portfolio();
                if (!string.IsNullOrWhiteSpace(portfolio.Item1))
                {
                    await _teleService.SendMessage(_telegramSettings.ChannelId, portfolio.Item1, portfolio.Item2);
                }
                _logger.LogInformation("Job PortfolioJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PortfolioJob execution failed");
            }
        }
    }
}
