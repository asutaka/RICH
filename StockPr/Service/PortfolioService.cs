using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface IPortfolioService
    {
        Task<(string, Dictionary<string, string>)> Portfolio();
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

        public async Task<(string, Dictionary<string, string>)> Portfolio()
        {
            var mes = string.Empty;
            var dic = new Dictionary<string, string>();
            try
            {
                var dt = DateTime.Now;
                if (dt.Day == 1 && dt.Hour == 8)
                {
                    mes = "*Quỹ đầu tư nước ngoài*";

                    var dc = await DragonCapital();
                    if (!string.IsNullOrWhiteSpace(dc))
                    {
                        dic.Add("Dragon Capital", dc);
                    }
                    var pyn = PynElite();
                    if (!string.IsNullOrWhiteSpace(pyn))
                    {
                        dic.Add("Pyn Elite", pyn);
                    }
                    if (dic.Any())
                    {
                        return (mes, dic);
                    }
                }

                if (dt.DayOfWeek == DayOfWeek.Monday && dt.Hour == 8)
                {
                    mes = "*Quỹ đầu tư trong nước*";

                    //Vinacapital 
                    var vmeef = Vinacapital(ESource.VinaCapital_VMEEF);
                    if (!string.IsNullOrWhiteSpace(vmeef))
                    {
                        dic.Add("VMEEF", vmeef);
                    }
                    var veof = Vinacapital(ESource.VinaCapital_VEOF);
                    if (!string.IsNullOrWhiteSpace(veof))
                    {
                        dic.Add("VEOF", veof);
                    }
                    var vesaf = Vinacapital(ESource.VinaCapital_VESAF);
                    if (!string.IsNullOrWhiteSpace(vesaf))
                    {
                        dic.Add("VESAF", vesaf);
                    }

                    var vcbf = VCBF();
                    if (!string.IsNullOrWhiteSpace(vcbf))
                    {
                        dic.Add("VCBF", vcbf);
                    }

                    var sgi = SGI();
                    if (!string.IsNullOrWhiteSpace(sgi))
                    {
                        dic.Add("SGI", sgi);
                    }

                    if(dic.Any())
                    {
                        return (mes, dic);
                    }    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.Portfolio|EXCEPTION| {ex.Message}");
            }
            return (null, null);
        }

        private string Vinacapital(ESource source)
        {
            try
            {
                var ty = (int)source;
                var dt = DateTime.Now;
                var time = $"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}";
                var builder = Builders<ConfigPortfolio>.Filter;
                var entityValid = _configRepo.GetEntityByFilter(builder.And(
                    builder.Eq(x => x.ty, ty),
                   builder.Eq(x => x.key, time)
                ));
                if (entityValid != null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = time,
                    ty = ty
                });

                return $"https://vinacapital.com/vi/investment-solutions/onshore-funds/{source.GetDisplayName()}/";
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.Vinacapital|EXCEPTION| {ex.Message}");
            }

            return string.Empty;
        }

        private string VCBF()
        {
            try
            {
                var ty = (int)ESource.VCBF;
                var dt = DateTime.Now;
                var time = $"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}";
                var builder = Builders<ConfigPortfolio>.Filter;
                var entityValid = _configRepo.GetEntityByFilter(builder.And(
                    builder.Eq(x => x.ty, ty),
                    builder.Eq(x => x.key, time)
                ));
                if (entityValid != null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = time,
                    ty = ty
                });

                return "https://www.vcbf.com/don-tai-lieu-quy/ban-thong-tin-quy/?p=1";
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.VCBF|EXCEPTION| {ex.Message}");
            }

            return string.Empty;
        }

        private string SGI()
        {
            try
            {
                var ty = (int)ESource.SGI;
                var dt = DateTime.Now;
                var time = $"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}";
                var builder = Builders<ConfigPortfolio>.Filter;
                var entityValid = _configRepo.GetEntityByFilter(builder.And(
                    builder.Eq(x => x.ty, ty),
                    builder.Eq(x => x.key, time)
                ));
                if (entityValid != null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = time,
                    ty = ty
                });

                return "https://sgicapital.com.vn/download-category/the-ballad-fund-bao-cao-dinh-ky/";
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.VCBF|EXCEPTION| {ex.Message}");
            }

            return string.Empty;
        }

        private async Task<string> DragonCapital()
        {
            try
            {
                var ty = (int)ESource.DC;
                var dt = DateTime.Now.AddMonths(-1);
                var builder = Builders<ConfigPortfolio>.Filter;
                var entityValid = _configRepo.GetEntityByFilter(builder.And(
                    builder.Eq(x => x.ty, ty),
                    builder.Eq(x => x.key, $"{dt.Year}{dt.Month.To2Digit()}")
                ));

                if (entityValid != null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = $"{dt.Year}{dt.Month.To2Digit()}",
                    ty = ty
                });

                return "https://www.veil.uk/the-fund/";
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.DragonCapital|EXCEPTION| {ex.Message}");
            }

            return string.Empty;
        }

        private string PynElite()
        {
            try
            {
                var ty = (int)ESource.PynElite;
                var dt = DateTime.Now.AddMonths(-1);

                var builder = Builders<ConfigPortfolio>.Filter;
                var entityValid = _configRepo.GetEntityByFilter(builder.And(
                    builder.Eq(x => x.ty, ty),
                    builder.Eq(x => x.key, $"{dt.Year}{dt.Month.To2Digit()}")
                ));

                if (entityValid != null)
                    return null;

                _configRepo.InsertOne(new ConfigPortfolio
                {
                    key = $"{dt.Year}{dt.Month.To2Digit()}",
                    ty = ty
                });
                return "https://www.pyn.fi/en/pyn-elite-fund/portfolio/";
            }
            catch (Exception ex)
            {
                _logger.LogError($"PortfolioService.PynElite|EXCEPTION| {ex.Message}");
            }

            return string.Empty;
        }
    }
}
