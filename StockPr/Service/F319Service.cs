using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Model.BCPT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPr.Service
{
    public interface IF319Service
    {
        Task<List<F319Model>> F319KOL();
    }
    public class F319Service : IF319Service
    {
        private readonly ILogger<F319Service> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigF319Repo _f319Repo;
        private readonly IF319AccountRepo _accRepo;
        public F319Service(ILogger<F319Service> logger, IAPIService apiService, IConfigF319Repo f319Repo, IF319AccountRepo accRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _f319Repo = f319Repo;
            _accRepo = accRepo;
        }

        public async Task<List<F319Model>> F319KOL()
        {
            var lOutput = new List<F319Model>();
            try
            {
                var lAcc = _accRepo.GetAll();
                lAcc = lAcc.Where(x => x.status >= 0).OrderBy(x => x.rank).ToList();
                foreach (var acc in lAcc)
                {
                    try
                    {
                        var lDat = await _apiService.F319_Scout(acc.url);
                        var lDatClean = Handle(lDat, acc.name);
                        if (lDatClean.Any())
                        {
                            lOutput.AddRange(lDatClean);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"F319Service.F319KOL|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }

        private List<F319Model> Handle(List<F319Model> param, string userName)
        {
            var lOutput = new List<F319Model>();
            var builder = Builders<ConfigF319>.Filter;
            try
            {
                foreach ( var item in param )
                {
                    var config = _f319Repo.GetEntityByFilter(builder.And(
                       builder.Eq(x => x.d, item.TimePost),
                       builder.Gte(x => x.user, userName)
                    ));

                    if(config != null)
                    {
                        continue;
                    }

                    _f319Repo.InsertOne(new ConfigF319
                    {
                        d = item.TimePost,
                        user = userName
                    });

                    item.Message = $"[{item.Title.Replace("\"","").Replace("“","").Replace("”","").Replace("[", "").Replace("]","")}](https://f319.com/{item.Url})|{userName}:{item.Content}";
                    lOutput.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"F319Service.Handle|EXCEPTION| {ex.Message}");
            }
            return lOutput;
        }
    }
}
