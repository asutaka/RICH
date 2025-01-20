using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IPortfolioService
    {
        Task<string> Portfolio();
    }
    public class PortfolioService : IPortfolioService
    {
        private readonly ILogger<PortfolioService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigPortfolioRepo _configRepo;
        public PortfolioService(ILogger<PortfolioService> logger, IAPIService apiService, IConfigPortfolioRepo configRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
        }

        public async Task<string> Portfolio()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                if (dt.Day >= 9 && dt.Day <= 20)
                {
                    //Vinacapital 
                    var vina = await Vinacapital();
                    if (!string.IsNullOrWhiteSpace(vina))
                    {
                        sBuilder.Append(vina);
                    }
                }
               
                if(dt.Day >= 28 || dt.Day <= 2)
                {
                    //Dragon Capital + Pyn Elite
                    var dc = await DragonCapital();
                    if (!string.IsNullOrWhiteSpace(dc))
                    {
                        sBuilder.Append(dc);
                    }
                }
                
                if(dt.Month % 3 == 1 && dt.Day >= 10 && dt.Day <= 25)
                {
                    //VCBF
                    var vcbf = await VCBF();
                    if (!string.IsNullOrWhiteSpace(vcbf))
                    {
                        sBuilder.Append(vcbf);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.Portfolio|EXCEPTION| {ex.Message}");
            }
            return sBuilder.ToString();
        }

        private async Task<string> Vinacapital()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                FilterDefinition<ConfigPortfolio> filter = null;
                var builder = Builders<ConfigPortfolio>.Filter;
                var lFilter = new List<FilterDefinition<ConfigPortfolio>>()
                            {
                                builder.Eq(x => x.ty, (int)ESource.VinaCapital),
                                builder.Eq(x => x.key, $"{dt.Year}{dt.Month.To2Digit()}"),
                            };
                foreach (var item in lFilter)
                {
                    if (filter is null)
                    {
                        filter = item;
                        continue;
                    }
                    filter &= item;
                }
                var entityValid = _configRepo.GetEntityByFilter(filter);
                if (entityValid != null)
                    return null;

                //Vinacapital - VEOF
                var veof = await _apiService.VinaCapital_Portfolio();
                if (veof is null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = $"{dt.Year}{dt.Month.To2Digit()}",
                    ty = (int)ESource.VinaCapital
                });
                //Vinacapital - VESAF
                var vesaf = await _apiService.VinaCapital_Portfolio(1);
                //Vinacapital - VMEEF
                var vmeef = await _apiService.VinaCapital_Portfolio(2);

                sBuilder.AppendLine($"{veof.title}({veof.path})");
                sBuilder.AppendLine($"{vesaf.title}({vesaf.path})");
                sBuilder.AppendLine($"{vmeef.title}({vesaf.path})");
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.Vinacapital|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> DragonCapital()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                if (dt.Day <= 20)
                {
                    dt = dt.AddMonths(-1);
                }

                FilterDefinition<ConfigPortfolio> filter = null;
                var builder = Builders<ConfigPortfolio>.Filter;
                var lFilter = new List<FilterDefinition<ConfigPortfolio>>()
                            {
                                builder.Eq(x => x.ty, (int)ESource.DC),
                                builder.Eq(x => x.key, $"{dt.Year}{dt.Month.To2Digit()}"),
                            };
                foreach (var item in lFilter)
                {
                    if (filter is null)
                    {
                        filter = item;
                        continue;
                    }
                    filter &= item;
                }
                var entityValid = _configRepo.GetEntityByFilter(filter);
                if (entityValid != null)
                    return null;

                var dc = await _apiService.DragonCapital_Portfolio();
                if (dc is null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = $"{dt.Year}{dt.Month.To2Digit()}",
                    ty = (int)ESource.DC
                });

                sBuilder.AppendLine($"{dc.title}({dc.path})");
                sBuilder.AppendLine($"Pyn Elite - Tháng {dt.Month}(https://www.pyn.fi/en/pyn-elite-fund/portfolio/)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.DragonCapital|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> VCBF()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                if (dt.Day <= 20)
                {
                    dt = dt.AddMonths(-1);
                }

                FilterDefinition<ConfigPortfolio> filter = null;
                var builder = Builders<ConfigPortfolio>.Filter;
                var lFilter = new List<FilterDefinition<ConfigPortfolio>>()
                            {
                                builder.Eq(x => x.ty, (int)ESource.VCBF),
                                builder.Eq(x => x.key, $"{dt.Year}{dt.Month.To2Digit()}"),
                            };
                foreach (var item in lFilter)
                {
                    if (filter is null)
                    {
                        filter = item;
                        continue;
                    }
                    filter &= item;
                }
                var entityValid = _configRepo.GetEntityByFilter(filter);
                if (entityValid != null)
                    return null;

                var lvcbf = await _apiService.VCBF_Portfolio();
                if (lvcbf is null || !lvcbf.Any())
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = $"{dt.Year}{dt.Month.To2Digit()}",
                    ty = (int)ESource.VCBF
                });

                foreach (var item in lvcbf)
                {
                    sBuilder.AppendLine($"{item.title}({item.path})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.VCBF|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }
    }
}
