using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface IBaoCaoTaiChinhService
    {
        Task<bool> CheckVietStockToken();
        Task SyncBCTCAll(bool isOverride = true);
    }
    public partial class BaoCaoTaiChinhService : IBaoCaoTaiChinhService
    {
        private readonly ILogger<BaoCaoTaiChinhService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IFinancialRepo _financialRepo;
        private readonly IStockRepo _stockRepo;
        public BaoCaoTaiChinhService(ILogger<BaoCaoTaiChinhService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo,
                                    IFinancialRepo financialRepo,
                                    IStockRepo stockRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
            _financialRepo = financialRepo;
            _stockRepo = stockRepo;
        }

        public async Task<bool> CheckVietStockToken()
        {
            try
            {
                var dt = DateTime.Now;
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                var mode = EConfigDataType.CheckVietStockToken;
                var builder = Builders<ConfigData>.Filter;
                var filter = builder.Eq(x => x.ty, (int)mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return true;
                }

                var lReportID = await _apiService.VietStock_KQKD_GetListReportData("ACB");

                var last = lConfig.LastOrDefault();
                if (last is null)
                {
                    _configRepo.InsertOne(new ConfigData
                    {
                        ty = (int)mode,
                        t = t
                    });
                }
                else
                {
                    last.t = t;
                    _configRepo.Update(last);
                }

                if (lReportID is null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.CheckVietStockToken|EXCEPTION| {ex.Message}");
            }

            return false;
        }

        public async Task SyncBCTCAll(bool isOverride = true)
        {
            try
            {
                await SyncBCTC_NganHang(isOverride);
                await SyncBCTC_ChungKhoan(isOverride);
                await SyncBCTC_BatDongSan(isOverride);
                await SyncBCTC(isOverride);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.SyncBCTC|EXCEPTION| {ex.Message}");
            }
        }

        private (long, long, long) GetCurrentTime()
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
