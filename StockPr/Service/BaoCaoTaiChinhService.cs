using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface IBaoCaoTaiChinhService
    {
        Task SyncBCTCAll(bool isOverride = true);
    }
    public partial class BaoCaoTaiChinhService : IBaoCaoTaiChinhService
    {
        private readonly ILogger<BaoCaoTaiChinhService> _logger;
        private readonly IVietstockService _vietstockService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IFinancialRepo _financialRepo;
        private readonly IStockRepo _stockRepo;
        public BaoCaoTaiChinhService(ILogger<BaoCaoTaiChinhService> logger,
                                    IVietstockService vietstockService,
                                    IConfigDataRepo configRepo,
                                    IFinancialRepo financialRepo,
                                    IStockRepo stockRepo)
        {
            _logger = logger;
            _vietstockService = vietstockService;
            _configRepo = configRepo;
            _financialRepo = financialRepo;
            _stockRepo = stockRepo;
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
