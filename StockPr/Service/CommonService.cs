using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface ICommonService
    {
        (long, long, long) GetCurrentTime();
    }
    public class CommonService : ICommonService
    {
        private readonly ILogger<CommonService> _logger;
        private readonly IConfigDataRepo _configRepo;
        public CommonService(ILogger<CommonService> logger, IConfigDataRepo configDataRepo)
        {
            _logger = logger;
            _configRepo = configDataRepo;
        }

        public (long, long, long) GetCurrentTime()
        {
            var dt = DateTime.Now;
            if (StaticVal._currentTime.Item1 <= 0
                || (long.Parse($"{dt.Year}{dt.Month}") != StaticVal._currentTime.Item4))
            {
                var filter = Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.CurrentTime);
                var eTime = _configRepo.GetEntityByFilter(filter);
                var eYear = eTime.t / 10;
                var eQuarter = eTime.t - eYear * 10;
                StaticVal._currentTime = (eTime.t, eYear, eQuarter, long.Parse($"{eYear}{dt.Month}"));
            }

            return (StaticVal._currentTime.Item1, StaticVal._currentTime.Item2, StaticVal._currentTime.Item3);
        }
    }
}
