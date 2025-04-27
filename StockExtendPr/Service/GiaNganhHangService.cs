using MongoDB.Driver;
using StockExtendPr.DAL;
using StockExtendPr.DAL.Entity;
using StockExtendPr.Model;
using StockExtendPr.Utils;

namespace StockExtendPr.Service
{
    public interface IGiaNganhHangService
    {
        Task TraceGia(bool isAll);
    }
    public class GiaNganhHangService : IGiaNganhHangService
    {
        private readonly ILogger<GiaNganhHangService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IMacroMicroRepo _macromicroRepo;
        private readonly int _flag = 7;
        public GiaNganhHangService(ILogger<GiaNganhHangService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo,
                                    IMacroMicroRepo macromicroRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
            _macromicroRepo = macromicroRepo;
        }
        public async Task TraceGia(bool isAll)
        {
            try
            {
                var lTraceGia = new List<TraceGiaModel>();
                var mode = EConfigDataType.MacroMicro;
                var dt = DateTime.Now;
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}{dt.Hour.To2Digit()}");
                var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, (int)mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return;

                    _configRepo.DeleteMany(filter);
                }
                //WCI
                var wci_key = "44756";
                var wci = await MacroMicro(isAll, wci_key);
                if (wci != null && wci.price > 0)
                {
                    lTraceGia.Add(wci);
                    var builderM = Builders<MacroMicro>.Filter;
                    var filterM = builderM.Eq(x => x.key, wci_key);
                    var lMacroMicro = _macromicroRepo.GetByFilter(filterM);
                    var last = lMacroMicro.LastOrDefault();
                    if (last is null)
                    {
                        _macromicroRepo.InsertOne(new MacroMicro
                        {
                            s = "wci",
                            key = wci_key,
                            price = (double)Math.Round(wci.price, 1),
                            W = (double)Math.Round(wci.weekly, 1),
                            M = (double)Math.Round(wci.monthly, 1),
                            Y = (double)Math.Round(wci.yearly, 1),
                            YTD = (double)Math.Round(wci.YTD, 1),
                            t = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        });
                    }
                    else
                    {
                        last.key = wci_key;
                        last.price = (double)Math.Round(wci.price, 1);
                        last.W = (double)Math.Round(wci.weekly, 1);
                        last.M = (double)Math.Round(wci.monthly, 1);
                        last.Y = (double)Math.Round(wci.yearly, 1);
                        last.YTD = (double)Math.Round(wci.YTD, 1);
                        last.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        _macromicroRepo.Update(last);
                    }
                }

                //BDTI
                var bdti_key = "946";
                var bdti = await MacroMicro(isAll, bdti_key);
                if (bdti != null && bdti.price > 0)
                {
                    lTraceGia.Add(bdti);
                    var builderM = Builders<MacroMicro>.Filter;
                    var filterM = builderM.Eq(x => x.key, bdti_key);
                    var lMacroMicro = _macromicroRepo.GetByFilter(filterM);
                    var last = lMacroMicro.LastOrDefault();
                    if (last is null)
                    {
                        _macromicroRepo.InsertOne(new MacroMicro
                        {
                            s = "bdti",
                            key = bdti_key,
                            price = (double)Math.Round(bdti.price, 1),
                            W = (double)Math.Round(bdti.weekly, 1),
                            M = (double)Math.Round(bdti.monthly, 1),
                            Y = (double)Math.Round(bdti.yearly, 1),
                            YTD = (double)Math.Round(bdti.YTD, 1),
                            t = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        });
                    }
                    else
                    {
                        last.key = bdti_key;
                        last.price = (double)Math.Round(bdti.price, 1);
                        last.W = (double)Math.Round(bdti.weekly, 1);
                        last.M = (double)Math.Round(bdti.monthly, 1);
                        last.Y = (double)Math.Round(bdti.yearly, 1);
                        last.YTD = (double)Math.Round(bdti.YTD, 1);
                        last.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        _macromicroRepo.Update(last);
                    }
                }

                if (lTraceGia.Any())
                {
                    _configRepo.InsertOne(new ConfigData
                    {
                        ty = (int)mode,
                        t = t
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.TraceGia|EXCEPTION| {ex.Message}");
            }
        }
        private async Task<TraceGiaModel> MacroMicro(bool isAll, string key)
        {
            try
            {
                //44756 946
                var dt = DateTime.Now;
                var wci = await _apiService.MacroMicro_WCI(key);
                var modelWCI = new TraceGiaModel();
                if (key.Equals("44756"))
                {
                    modelWCI.content = "Cước Container";
                    modelWCI.description = "HAH";
                }
                else
                {
                    modelWCI.content = "Cước vận tải dầu thô";
                    modelWCI.description = "PVT,VTO";
                }

                var isPrintWCI = false;
                if (wci != null)
                {

                    var composite = wci.series.FirstOrDefault();
                    if (composite != null)
                    {
                        var lData = composite.Select(x => new MacroMicro_CleanData
                        {
                            Date = x[0].ToDateTime("yyyy-MM-dd"),
                            Value = decimal.Parse(x[1])
                        });
                        if (lData.Any())
                        {
                            var last = lData.Last();
                            modelWCI.price = last.Value;
                            //weekly
                            var dtPrev = dt.AddDays(-2);
                            if (last.Date >= dtPrev)
                            {
                                var lastWeek = lData.SkipLast(1).Last();
                                var rateWeek = Math.Round(100 * (-1 + last.Value / lastWeek.Value), 1);
                                modelWCI.weekly = rateWeek;
                                if (rateWeek >= _flag || rateWeek <= -_flag)
                                {
                                    isPrintWCI = true;
                                }
                            }
                            //Monthly
                            var dtMonthly = dt.AddMonths(-1);
                            var itemMonthly = lData.Where(x => x.Date <= dtMonthly).OrderByDescending(x => x.Date).FirstOrDefault();
                            if (itemMonthly != null)
                            {
                                var rateMonthly = Math.Round(100 * (-1 + last.Value / itemMonthly.Value), 1);
                                modelWCI.monthly = rateMonthly;
                            }
                            //yearly
                            var dtYearly = dt.AddYears(-1);
                            var itemYearly = lData.Where(x => x.Date <= dtYearly).OrderByDescending(x => x.Date).FirstOrDefault();
                            if (itemYearly != null)
                            {
                                var rateYearly = Math.Round(100 * (-1 + last.Value / itemYearly.Value), 1);
                                modelWCI.yearly = rateYearly;
                            }
                            //YTD
                            var dtYTD = new DateTime(dt.Year, 1, 2);
                            var itemYTD = lData.Where(x => x.Date <= dtYTD).OrderByDescending(x => x.Date).FirstOrDefault();
                            if (itemYTD != null)
                            {
                                var rateYTD = Math.Round(100 * (-1 + last.Value / itemYTD.Value), 1);
                                modelWCI.YTD = rateYTD;
                            }
                        }
                    }
                }
                //Print
                if (isAll || isPrintWCI)
                {
                    return modelWCI;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.MacroMicro|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
