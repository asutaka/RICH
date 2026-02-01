using Quartz;
using StockPr.Service;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class EPSRankJob : IJob
    {
        private readonly ILogger<EPSRankJob> _logger;
        private readonly IEPSRankService _epsService;

        public EPSRankJob(ILogger<EPSRankJob> logger, IEPSRankService epsService)
        {
            _logger = logger;
            _epsService = epsService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job EPSRankJob started.");
                await _epsService.RankMaCKSync();
                _logger.LogInformation("Job EPSRankJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EPSRankJob execution failed");
            }
        }
    }
}
