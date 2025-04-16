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
        public F319Service(ILogger<F319Service> logger, IAPIService apiService, IConfigF319Repo f319Repo)
        {
            _logger = logger;
            _apiService = apiService;
            _f319Repo = f319Repo;
        }

        public async Task<List<F319Model>> F319KOL()
        {
            var lOutput = new List<F319Model>();
            try
            {
                var lKOL1 = await _apiService.F319_Scout("trungken18.701187");//Nghiên cứu sâu
                var lKOL1Clean = Handle(lKOL1, "Trung kiên");
                if (lKOL1Clean.Any())
                {
                    lOutput.AddRange(lKOL1Clean);
                }

                var lKOL2 = await _apiService.F319_Scout("livermore888.497341");//Nhạy bén
                var lKOL2Clean = Handle(lKOL2, "Livermore");
                if (lKOL2Clean.Any())
                {
                    lOutput.AddRange(lKOL2Clean);
                }

                var lKOL3 = await _apiService.F319_Scout("s400.169627");//Chuyên đánh theo bảng điện
                var lKOL3Clean = Handle(lKOL3, "S400");
                if (lKOL3Clean.Any())
                {
                    lOutput.AddRange(lKOL3Clean);
                }

                var lKOL4 = await _apiService.F319_Scout("soigia1997.530085");//FA tốt
                var lKOL4Clean = Handle(lKOL4, "Sói già 1997");
                if (lKOL4Clean.Any())
                {
                    lOutput.AddRange(lKOL4Clean);
                }

                var lKOL5 = await _apiService.F319_Scout("f3192006.435540");//Gần như là quen lái DPG
                var lKOL5Clean = Handle(lKOL5, "f3192006");
                if (lKOL5Clean.Any())
                {
                    lOutput.AddRange(lKOL5Clean);
                }

                var lKOL6 = await _apiService.F319_Scout("xigalahabana.504787");//Chuyên BDS
                var lKOL6Clean = Handle(lKOL6, "xigalahabana");
                if (lKOL6Clean.Any())
                {
                    lOutput.AddRange(lKOL6Clean);
                }

                var lKOL7 = await _apiService.F319_Scout("cuongnb89.704953");//Lựa hàng tốt
                var lKOL7Clean = Handle(lKOL7, "Cường NB");
                if (lKOL7Clean.Any())
                {
                    lOutput.AddRange(lKOL7Clean);
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

                    item.Message = $"[{item.Title}](https://f319.com/{item.Url})|{userName}:{item.Content}";
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
