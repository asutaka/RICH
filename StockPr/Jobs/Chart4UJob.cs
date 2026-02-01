using MongoDB.Driver;
using Quartz;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Service;

namespace StockPr.Jobs
{
    [DisallowConcurrentExecution]
    public class Chart4UJob : IJob
    {
        private readonly ILogger<Chart4UJob> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly IChartService _chartService;
        private readonly ITeleService _teleService;
        private readonly IAccountRepo _accountRepo;

        public Chart4UJob(
            ILogger<Chart4UJob> logger,
            IAnalyzeService analyzeService,
            IChartService chartService,
            ITeleService teleService,
            IAccountRepo accountRepo)
        {
            _logger = logger;
            _analyzeService = analyzeService;
            _chartService = chartService;
            _teleService = teleService;
            _accountRepo = accountRepo;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job Chart4UJob started.");
                var res = _analyzeService.Chart_4U();
                if (!res)
                {
                    _logger.LogInformation("Job Chart4UJob: Chart_4U returned false. Skipping.");
                    return;
                }

                var dic = new Dictionary<string, Stream>();
                var lAccount = _accountRepo.GetByFilter(Builders<Account>.Filter.Gt(x => x.status, 0));
                
                foreach (var item in lAccount)
                {
                    if (item.list is null)
                        item.list = new List<string>();

                    foreach (var itemMaCK in item.list)
                    {
                        try
                        {
                            Stream stream;
                            if (dic.ContainsKey(itemMaCK))
                            {
                                stream = dic[itemMaCK];
                            }
                            else
                            {
                                stream = await _chartService.Chart_ThongKeKhopLenh(itemMaCK);
                                if (stream != null)
                                {
                                    dic.Add(itemMaCK, stream);
                                }
                            }

                            if (stream != null)
                            {
                                await _teleService.SendPhoto(item.u, stream);
                                await Task.Delay(500);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Chart4UJob|ACCOUNT| {item.u}|EXCEPTION| {ex.Message}");
                        }
                    }
                }
                
                _logger.LogInformation("Job Chart4UJob completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chart4UJob execution failed");
            }
        }
    }
}
