using Bybit.Net.Enums;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using MongoDB.Driver;
using Skender.Stock.Indicators;

namespace CoinPr.Service
{
    public interface IPrepareService
    {
        Task CheckWycKoffPrepare();
    }
    public class PrepareService : IPrepareService
    {
        private readonly ILogger<PrepareService> _logger;
        private readonly IPrepareRepo _preRepo;
        private readonly IAPIService _apiService;

        public PrepareService(ILogger<PrepareService> logger, IAPIService apiService, IPrepareRepo preRepo)
        {
            _logger = logger;
            _preRepo = preRepo;
            _apiService = apiService;
        }

        public async Task CheckWycKoffPrepare()
        {
            try
            {
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.Skip(0).Take(1000);

                var lPrepare = _preRepo.GetAll();

                foreach (var item in lTake)
                {
                    try
                    {
                        if (lPrepare.Any(x => x.s == item && x.status == (int)EStatus.Active))
                            continue;

                        var l1H = await _apiService.GetData_Bybit_1H(item);
                        //var lbb = l1H.GetBollingerBands();
                        //var count = l1H.Count();
                        //var timeFlag = DateTime.MinValue;
                        //for (int i = 100; i < count; i++)
                        //{
                        //    var lDat = l1H.Take(i).ToList();
                        //    var last = lDat.Last();
                        //    if (last.Date < timeFlag)
                        //        continue;

                            var rs = l1H.IsWyckoff();
                            if (rs != null)
                            {

                                var builder = Builders<Prepare>.Filter;
                                var lSignal = _preRepo.GetByFilter(builder.And(
                                    builder.Eq(x => x.s, item),
                                    builder.Eq(x => x.sos.Date, rs.sos.Date)
                                ));
                                if (lSignal.Any())
                                    continue;
                                var prepare = new Prepare
                                {
                                    s = item,
                                    ex = (int)EExchange.Bybit,
                                    t = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                    status = (int)EStatus.Active,
                                    side = (int)OrderSide.Buy,
                                    sos = rs.sos,
                                    signal = rs.signal
                                };
                                _preRepo.InsertOne(prepare);
                            }
                        //}
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
