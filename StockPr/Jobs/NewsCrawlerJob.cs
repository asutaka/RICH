using Microsoft.Extensions.Options;
using Quartz;
using StockPr.Service;
using StockPr.Settings;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class NewsCrawlerJob : IJob
    {
        private readonly ILogger<NewsCrawlerJob> _logger;
        private readonly INewsService _newsService;
        private readonly ITeleService _teleService;
        private readonly TelegramSettings _telegramSettings;

        public NewsCrawlerJob(
            ILogger<NewsCrawlerJob> logger,
            INewsService newsService,
            ITeleService teleService,
            IOptions<TelegramSettings> telegramSettings)
        {
            _logger = logger;
            _newsService = newsService;
            _teleService = teleService;
            _telegramSettings = telegramSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job NewsCrawlerJob started.");
                var news = await _newsService.GetNews();
                if (news.Any())
                {
                    await _teleService.SendMessages(_telegramSettings.ChannelNewsId, news, true);
                }
                _logger.LogInformation("Job NewsCrawlerJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NewsCrawlerJob execution failed");
            }
        }
    }
}
