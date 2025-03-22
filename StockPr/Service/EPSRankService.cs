using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IEPSRankService
    {
        Task<(int, string)> RankEPS(DateTime dt);
        Task<(decimal, decimal)> FreeFloat(string s);
    }
    public class EPSRankService : IEPSRankService
    {
        private readonly ILogger<EPSRankService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        public EPSRankService(ILogger<EPSRankService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
        }
        /// <summary>
        /// FreeFloat + EPS 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public async Task<(decimal, decimal)> FreeFloat(string s)
        {
            try
            {
                var eps = await _apiService.SSI_GetFinanceStock(s);
                var freefloat = await _apiService.SSI_GetFreefloatStock(s);
                return (freefloat, eps);
            }
            catch (Exception ex)
            {
                _logger.LogError($"EPSRankService.FreeFloat|EXCEPTION| {ex.Message}");
            }
            return (0, 0);
        }
        public async Task<(int, string)> RankEPS(DateTime dt)
        {
            try
            {
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                FilterDefinition<ConfigData> filterConfig = Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.EPS);
                var lConfig = _configRepo.GetByFilter(filterConfig);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filterConfig);
                }

                var strOutput = new StringBuilder();
                strOutput.AppendLine("[Thống kê EPS]");
                var lEPS = new List<(string, decimal)>();
                foreach (var item in StaticVal._lStock)
                {
                    try
                    {
                        var eps = await _apiService.SSI_GetFinanceStock(item.s);
                        if(eps >= 5000)
                        {
                            lEPS.Add((item.s, Math.Round(eps)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AnalyzeService.RankEPS|EXCEPTION(RankEPS)| {ex.Message}");
                    }
                }
                lEPS = lEPS.OrderByDescending(x => x.Item2).Take(100).ToList();
                var index = 1;
                foreach (var item in lEPS)
                {
                    string mes = string.Empty;
                    var freefloat = await _apiService.SSI_GetFreefloatStock(item.Item1);
                    if(freefloat <= 10)
                    {
                        mes = $"{index++}. [{item.Item1}](https://finance.vietstock.vn/{item.Item1}/phan-tich-ky-thuat.htm) (eps: {item.Item2.ToString("#,##0")})";
                    }
                    else
                    {
                        mes = $"{index++}. [{item.Item1}](https://finance.vietstock.vn/{item.Item1}/phan-tich-ky-thuat.htm) (eps: {item.Item2.ToString("#,##0")} - freefloat: {freefloat}%)";
                    }
                    strOutput.AppendLine(mes);
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)EConfigDataType.EPS,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"EPSRankService.RankEPS|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }
    }
}
