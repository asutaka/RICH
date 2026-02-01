using MongoDB.Driver;
using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IAnalyzeService
    {
        Task<string> Realtime();
        Task<string> ThongKeGDNN_NhomNganh();
        Task<string> ThongKeTuDoanh();
        Task<(int, string, string, string)> ChiBaoKyThuat(DateTime dt, bool isSave);
        Task<(int, string)> ThongkeForeign_PhienSang(DateTime dt);
        Task<Stream> Chart_ThongKeKhopLenh(string sym = "10");
        Task<string> DetectEntry();
        bool Chart_4U();
        Task<(int, string)> AnalyzeSectorIndex();
    }
    public class AnalyzeService : IAnalyzeService
    {
        private readonly ILogger<AnalyzeService> _logger;
        private readonly IMarketDataService _marketDataService;
        private readonly IChartService _chartService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IStockRepo _stockRepo;
        private readonly ISymbolRepo _symbolRepo;
        private readonly ICategoryRepo _categoryRepo;
        private readonly IPreEntryRepo _preEntryRepo;
        
        public AnalyzeService(ILogger<AnalyzeService> logger,
                                    IMarketDataService marketDataService,
                                    IChartService chartService,
                                    IConfigDataRepo configRepo,
                                    IStockRepo stockRepo,
                                    ISymbolRepo symbolRepo,
                                    ICategoryRepo categoryRepo,
                                    IPreEntryRepo preEntryRepo) 
        {
            _logger = logger;
            _marketDataService = marketDataService;
            _chartService = chartService;
            _configRepo = configRepo;
            _stockRepo = stockRepo;
            _symbolRepo = symbolRepo;
            _categoryRepo = categoryRepo;
            _preEntryRepo = preEntryRepo;
        }

        public async Task<string> Realtime()
        {
            var sBuilder = new StringBuilder();
            //Chỉ báo cắt lên MA20
            try
            {
                var chibao = await ChiBaoMA20();
                if (chibao.Item1 > 0)
                {
                    sBuilder.AppendLine(chibao.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.Realtime|EXCEPTION(ChiBaoMA20)| {ex.Message}");
            }

            //Chỉ báo vượt đỉnh 52 Tuần(1 năm)
            try
            {
                var chibao = await ChiBao52W();
                if (chibao.Item1 > 0)
                {
                    sBuilder.AppendLine(chibao.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.Realtime|EXCEPTION(ChiBao52W)| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<(int, string)> ChiBaoMA20()
        {
            try
            {
                var strOutput = new StringBuilder();

                var lMa = await _marketDataService.Money24h_GetMaTheoChiBao("ma20");
                if (lMa is null
                    || !lMa.Any())
                    return (0, null);

                var lMaClean = lMa.Where(x => x.match_price > x.basic_price && x.accumylated_vol > 5000).OrderByDescending(x => Math.Abs(x.change_vol_percent_5));
                var lStock = _stockRepo.GetAll();
                var lStockClean = new List<Stock>();
                foreach (var item in lMaClean)
                {
                    var entityStock = lStock.FirstOrDefault(x => x.s == item.symbol && x.status > 0);
                    if (entityStock != null)
                        lStockClean.Add(entityStock);
                }

                if (!lStockClean.Any())
                    return (0, null);

                var lOut = new List<MaTheoPTKT24H_MA20>();
                foreach (var item in lStockClean)
                {
                    var lData = await _marketDataService.SSI_GetDataStock(item.s);
                    if (lData.Count() < 250
                        || lData.Last().Volume < 50000)
                        continue;

                    var lMa20 = lData.GetSma(20);
                    var lIchi = lData.GetIchimoku();

                    //Analyze
                    var entity = lData.Last();
                    var ma20 = lMa20.Last();
                    var entityNear = lData.SkipLast(1).TakeLast(1).First();
                    var ma20Near = lMa20.SkipLast(1).TakeLast(1).First();

                    if (entityNear.Open < (decimal)ma20Near.Sma
                        && entityNear.Close < (decimal)ma20Near.Sma
                        && entity.Close >= (decimal)ma20.Sma)
                    {
                        var model = new MaTheoPTKT24H_MA20
                        {
                            s = item.s,
                            rank = item.rank
                        };
                        var ichi = lIchi.Last();
                        if (entity.Close >= ichi.SenkouSpanA
                            && entity.Close >= ichi.SenkouSpanB)
                        {
                            model.isIchi = true;
                        }

                        foreach (var itemVolume in lData)
                        {
                            itemVolume.Close = itemVolume.Volume;
                        }
                        var lMa20Volume = lData.GetSma(20);
                        var vol = lMa20Volume.Last();

                        if (entity.Volume > (decimal)vol.Sma
                            && entity.Volume > entityNear.Volume)
                        {
                            model.isVol = true;
                        }

                        lOut.Add(model);
                    }
                }

                if (!lOut.Any())
                    return (0, null);

                strOutput.AppendLine($"[Thông báo] Top cổ phiếu vừa cắt lên MA20:");
                var index = 1;
                foreach (var item in lOut.OrderBy(x => x.rank).Take(10).ToList())
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s})";
                    var extend = string.Empty;
                    if (item.isIchi)
                    {
                        extend += "ichimoku";
                    }
                    if (item.isVol)
                    {
                        extend = (string.IsNullOrWhiteSpace(extend)) ? "vol đột biến" : $"{extend}, vol đột biến";
                    }
                    if (!string.IsNullOrWhiteSpace(extend))
                    {
                        content += $"- {extend}";
                    }
                    strOutput.AppendLine(content);
                    index++;
                }

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBaoMA20|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<(int, string)> ChiBao52W()
        {
            try
            {
                var strOutput = new StringBuilder();

                var lMa = await _marketDataService.Money24h_GetMaTheoChiBao("break_1y");
                if (lMa is null
                    || !lMa.Any())
                    return (0, null);

                var lMaClean = lMa.Where(x => x.match_price > x.basic_price && x.accumylated_vol > 5000).OrderByDescending(x => Math.Abs(x.change_vol_percent_5));
                var lStock = _stockRepo.GetAll();
                var lStockClean = new List<Stock>();
                foreach (var item in lMaClean)
                {
                    var entityStock = lStock.FirstOrDefault(x => x.s == item.symbol && x.status > 0);
                    if (entityStock != null)
                        lStockClean.Add(entityStock);
                }

                if (!lStockClean.Any())
                    return (0, null);

                strOutput.AppendLine($"[Thông báo] Top cổ phiếu vừa vượt đỉnh 52 Week:");
                var index = 1;
                foreach (var item in lStockClean.OrderBy(x => x.rank).Take(10).ToList())
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s})";
                    var extend = string.Empty;
                    if (!string.IsNullOrWhiteSpace(extend))
                    {
                        content += $"- {extend}";
                    }
                    strOutput.AppendLine(content);
                    index++;
                }

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBao52W|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        public async Task<string> ThongKeGDNN_NhomNganh()
        {
            var dt = DateTime.Now;
            var sBuilder = new StringBuilder();
            var nhomnganh = await ThongkeNhomNganh(dt);
            if (nhomnganh.Item1 > 0)
            {
                sBuilder.AppendLine(nhomnganh.Item2);
            }

            var foreign = await ThongkeForeign(dt);
            if(foreign.Item1 > 0)
            {
                sBuilder.AppendLine(foreign.Item2);
                sBuilder.AppendLine();
            }

            var foreignWeek = await ThongkeForeignWeek(dt);
            if (foreignWeek.Item1 > 0)
            {
                sBuilder.AppendLine(foreignWeek.Item2);
                sBuilder.AppendLine();
            }

            return sBuilder.ToString();
        }

        public async Task<string> ThongKeTuDoanh()
        {
            var dt = DateTime.Now;
            var sBuilder = new StringBuilder();
            var today = await ThongkeTuDoanh(dt);
            if (today.Item1 > 0)
            {
                sBuilder.AppendLine(today.Item2);
                sBuilder.AppendLine();

                var week = await ThongkeTuDoanhWeek(dt);
                if (week.Item1 > 0)
                {
                    sBuilder.AppendLine(week.Item2);
                }
            }
          
            return sBuilder.ToString();
        }

        public async Task<string> DetectEntry()
        {
            try
            {
                var dt = DateTime.Now;
                var ty = (int)EConfigDataType.Entry;
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                FilterDefinition<ConfigData> filterConfig = Builders<ConfigData>.Filter.Eq(x => x.ty, ty);
                var lConfig = _configRepo.GetByFilter(filterConfig);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return string.Empty;

                    _configRepo.DeleteMany(filterConfig);
                }

                var lSym = _symbolRepo.GetAll();
                var strOutput = new StringBuilder();
                var lReport = new List<EntryModel>();
                foreach (var symbol in lSym.Select(x => x.s))
                {
                    try
                    {
                        var lInfo = await _marketDataService.SSI_GetStockInfo(symbol, dt.AddDays(-30), dt);
                        if(lInfo.data.Count < 5)
                            continue;
                        foreach (var item in lInfo.data)
                        {
                            var date = DateTime.ParseExact(item.tradingDate, "dd/MM/yyyy", null);
                            item.TimeStamp = new DateTimeOffset(date.Date, TimeSpan.Zero).ToUnixTimeSeconds();
                        }
                        var filter = Builders<PreEntry>.Filter.Eq(x => x.s, symbol);
                        var pre = _preEntryRepo.GetEntityByFilter(filter);
                        var lData = (await _marketDataService.SSI_GetDataStockT(symbol)).DistinctBy(x => x.Date).ToList();
                        lData.Remove(lData.Last());
                        if(lData.Count < 50)
                            continue;
                        var res = lData.CheckEntry(lInfo, pre);
                        if (res.preAction == EPreEntryAction.DELETE)
                        {
                            if (pre != null)
                            {
                                _preEntryRepo.DeleteMany(filter);
                            }
                        }
                        else if (res.preAction == EPreEntryAction.UPDATE)
                        {
                            if ((!res.pre.isPrePressure && !res.pre.isPreNN1))
                            {
                                if (pre != null)
                                {
                                    _preEntryRepo.DeleteMany(filter);
                                }
                            }
                            else
                            {
                                if (pre is null)
                                {
                                    res.pre.s = symbol;
                                    _preEntryRepo.InsertOne(res.pre);
                                }
                                else
                                {
                                    _preEntryRepo.Update(res.pre);
                                }
                            }
                        }
                        if (res.Response > 0)
                        {
                            res.s = symbol;
                            lReport.Add(res);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AnalyzeService.DetectEntry|EXCEPTION(DetectEntry)| {ex.Message}");
                    }
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = ty,
                    t = t
                });
                var count = lReport.Count;
                if(count <= 0)
                {
                    return string.Empty;
                }

                strOutput.AppendLine($"*Tín hiệu*");
                var index = 1;
                foreach (var res in lReport.OrderByDescending(x => x.Response).Take(15))
                {
                    var signal = string.Empty;
                    if ((res.Response & EEntry.RSI) == EEntry.RSI)
                    {
                        // Có RSI
                        signal += "RSI|";
                    }
                    if ((res.Response & EEntry.PRESSURE) == EEntry.PRESSURE)
                    {
                        // Có Pressure
                        signal += "Pressure|";
                    }
                    if ((res.Response & EEntry.NN1) == EEntry.NN1)
                    {
                        // Có NN1
                        signal += "NN1(Revert)|";
                    }

                    if ((res.Response & EEntry.NN2) == EEntry.NN2)
                    {
                        // Có NN1
                        signal += "NN2(Green)|";
                    }
                    if ((res.Response & EEntry.NN3) == EEntry.NN3)
                    {
                        // Có NN1
                        signal += "NN3(Red)|";
                    }
                    if ((res.Response & EEntry.WYCKOFF) == EEntry.WYCKOFF)
                    {
                        // Có NN1
                        signal += "Wyckoff|";
                    }
                    var content = $"{index++}. [{res.s}](https://fireant.vn/ma-chung-khoan/{res.s}) - {signal.TrimEnd('|')}";
                    strOutput.AppendLine(content);
                }

                return strOutput.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.DetectEntry|EXCEPTION| {ex.Message}");
            }

            return string.Empty;
        }

        private async Task<(int, string)> ThongkeForeign(DateTime dt)
        {
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var type = EMoney24hTimeType.today;
                var mode = (int)EConfigDataType.GDNN_today;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var strOutput = new StringBuilder();
                var lData = new List<Money24h_ForeignResponse>();
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.HSX, type));
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.HNX, type));
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.UPCOM, type));
                if (!lData.Any())
                    return (0, null);

                var head = $"*GDNN ngày {dt.ToString("dd/MM/yyyy")}*"; 
                strOutput.AppendLine(head);
                strOutput.AppendLine();
                strOutput.AppendLine($">>Top mua ròng");
                var lTopBuy = lData.OrderByDescending(x => x.net_val).Take(10);
                var lTopSell = lData.OrderBy(x => x.net_val).Take(10);
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = mode,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ThongkeForeign|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<(int, string)> ThongkeForeignWeek(DateTime dt)
        {
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var type = EMoney24hTimeType.week;
                var mode = (int)EConfigDataType.GDNN_week;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var strOutput = new StringBuilder();
                var lData = new List<Money24h_ForeignResponse>();
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.HSX, type));
                if (!lData.Any())
                    return (0, null);

                var head = $"*GDNN 7 ngày gần nhất*"; 
                strOutput.AppendLine(head);
                strOutput.AppendLine();
                strOutput.AppendLine($">>Top mua ròng");
                var lTopBuy = lData.OrderByDescending(x => x.net_val).Take(10);
                var lTopSell = lData.OrderBy(x => x.net_val).Take(10);
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = mode,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ThongkeForeignWeek|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        public async Task<(int, string)> ThongkeForeign_PhienSang(DateTime dt)
        {
            try
            {
                var type = EMoney24hTimeType.today;
                var strOutput = new StringBuilder();
                var lData = new List<Money24h_ForeignResponse>();
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.HSX, type));
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.HNX, type));
                lData.AddRange(await _marketDataService.Money24h_GetForeign(EExchange.UPCOM, type));
                if (!lData.Any())
                    return (0, null);

                var head = $"*GDNN phiên sáng {dt.ToString("dd/MM/yyyy")}*"; ;
                strOutput.AppendLine(head);
                strOutput.AppendLine();
                strOutput.AppendLine($">>Top mua ròng");
                var lTopBuy = lData.OrderByDescending(x => x.net_val).Take(10);
                var lTopSell = lData.OrderBy(x => x.net_val).Take(10);
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.00")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.00")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ThongkeForeign_PhienSang|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<(int, string)> ThongkeTuDoanh(DateTime dt)
        {
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var type = EMoney24hTimeType.today;
                var mode = (int)EConfigDataType.TuDoanh_today;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var strOutput = new StringBuilder();
                var lData = new List<Money24h_TuDoanhResponse>();
                lData.AddRange(await _marketDataService.Money24h_GetTuDoanh(EExchange.HSX, type));
                if (!lData.Any())
                    return (0, null);
                var first = lData.First();
                var date = ((decimal)(first.d)).UnixTimeStampToDateTime(); 
                if(date.Month != dt.Month
                    || date.Day != dt.Day)
                    return (0, null);

                var head = $"*Tự doanh ngày {dt.ToString("dd/MM/yyyy")}*"; 
                strOutput.AppendLine(head);
                strOutput.AppendLine();
                strOutput.AppendLine($">>Top mua ròng");
                var lTopBuy = lData.OrderByDescending(x => x.prop_net).Take(10);
                var lTopSell = lData.OrderBy(x => x.prop_net).Take(10);
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Mua ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
                    if (item.prop_net_pt > 0)
                    {
                        content += $" - Thỏa thuận mua: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    else if (item.prop_net_pt < 0)
                    {
                        content += $" - Thỏa thuận bán: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Bán ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
                    if (item.prop_net_pt > 0)
                    {
                        content += $" - Thỏa thuận mua: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    else if (item.prop_net_pt < 0)
                    {
                        content += $" - Thỏa thuận bán: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    strOutput.AppendLine(content);
                    index++;
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = mode,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ThongkeForeignWeek|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<(int, string)> ThongkeTuDoanhWeek(DateTime dt)
        {
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var type = EMoney24hTimeType.week;
                var mode = (int)EConfigDataType.TuDoanh_week;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var strOutput = new StringBuilder();
                var lData = new List<Money24h_TuDoanhResponse>();
                lData.AddRange(await _marketDataService.Money24h_GetTuDoanh(EExchange.HSX, type));
                if (!lData.Any())
                    return (0, null);

                var head = $"*Tự doanh 7 ngày gần nhất*"; 
                strOutput.AppendLine(head);
                strOutput.AppendLine();
                strOutput.AppendLine($">>Top mua ròng");
                var lTopBuy = lData.OrderByDescending(x => x.prop_net).Take(10);
                var lTopSell = lData.OrderBy(x => x.prop_net).Take(10);
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Mua ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
                    if(item.prop_net_pt > 0)
                    {
                        content += $" - Thỏa thuận mua: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }  
                    else if (item.prop_net_pt < 0)
                    {
                        content += $" - Thỏa thuận bán: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s}) (Bán ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
                    if (item.prop_net_pt > 0)
                    {
                        content += $" - Thỏa thuận mua: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    else if (item.prop_net_pt < 0)
                    {
                        content += $" - Thỏa thuận bán: {Math.Abs(item.prop_net_pt).ToString("#,##0.#")} tỷ";
                    }
                    strOutput.AppendLine(content);
                    index++;
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = mode,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ThongkeForeignWeek|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<(int, string)> ThongkeNhomNganh(DateTime dt)
        {
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var type = EMoney24hTimeType.today;
                var mode = EConfigDataType.ThongKeNhomNganh_today;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, (int)mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var strOutput = new StringBuilder();

                var res = await _marketDataService.Money24h_GetNhomNganh(type);
                if (res is null
                    || !res.data.groups.Any()
                    || res.data.last_update < dTime)
                    return (0, null);

                var lData = _categoryRepo.GetAll();
                var lNhomNganhData = new List<Money24h_NhomNganh_GroupResponse>();
                foreach (var itemLv1 in res.data.groups)
                {
                    var findLv1 = lData.FirstOrDefault(x => x.code == itemLv1.icb_code);
                    if (findLv1 != null)
                    {
                        itemLv1.icb_name = findLv1.name;
                        lNhomNganhData.Add(itemLv1);
                        continue;
                    }

                    foreach (var itemLv2 in itemLv1.child)
                    {
                        var findLv2 = lData.FirstOrDefault(x => x.code == itemLv2.icb_code);
                        if (findLv2 != null)
                        {
                            itemLv2.icb_name = findLv2.name;
                            lNhomNganhData.Add(itemLv2);
                            continue;
                        }

                        foreach (var itemLv3 in itemLv2.child)
                        {
                            var findLv3 = lData.FirstOrDefault(x => x.code == itemLv3.icb_code);
                            if (findLv3 != null)
                            {
                                itemLv3.icb_name = findLv3.name;
                                lNhomNganhData.Add(itemLv3);
                                continue;
                            }

                            foreach (var itemLv4 in itemLv3.child)
                            {
                                var findLv4 = lData.FirstOrDefault(x => x.code == itemLv4.icb_code);
                                if (findLv4 != null)
                                {
                                    itemLv4.icb_name = findLv4.name;
                                    lNhomNganhData.Add(itemLv4);
                                }
                            }
                        }
                    }
                }

                if (!lNhomNganhData.Any())
                    return (0, null);

                lNhomNganhData = lNhomNganhData.OrderByDescending(x => (float)x.total_stock_increase / x.total_stock).Take(5).ToList();


                var head = $"*Nhóm ngành được quan tâm ngày {dt.ToString("dd/MM/yyyy")}*";
                strOutput.AppendLine(head);
                var index = 1;
                foreach (var item in lNhomNganhData)
                {
                    var content = $"{index}. {item.icb_name}({Math.Round((float)item.total_stock_increase * 100 / item.total_stock, 1)}%)";
                    strOutput.AppendLine(content);
                    index++;
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)mode,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ThongkeNhomNganh|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        public async Task<(int, string, string, string)> ChiBaoKyThuat(DateTime dt, bool isSave)
        {
            try
            {
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                if(isSave)
                {
                    FilterDefinition<ConfigData> filterConfig = Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.ChiBaoKyThuat);
                    var lConfig = _configRepo.GetByFilter(filterConfig);
                    if (lConfig.Any())
                    {
                        if (lConfig.Any(x => x.t == t))
                            return (0, null, null, null);

                        _configRepo.DeleteMany(filterConfig);
                    }
                }

                var lSym = _symbolRepo.GetAll();
                var strOutput = new StringBuilder();
                var lReport = new List<ReportPTKT>();
                //var filter = Builders<Stock>.Filter.Gte(x => x.status, 0);
                //var lStock = _stockRepo.GetByFilter(filter).OrderBy(x => x.rank);
                foreach (var item in lSym.Select(x => x.s))
                {
                    try
                    {
                        var model = await ChiBaoKyThuatOnlyStock(item, 500);
                        if (model is null)
                            continue;

                        lReport.Add(model);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AnalyzeService.ChiBaoKyThuat|EXCEPTION(ChiBaoKyThuatOnlyStock)| {ex.Message}");
                    }
                }

                var count = lReport.Count;
                //Tỉ lệ cp trên ma20 
                strOutput.AppendLine($"*Thống kê PTKT*");
                strOutput.AppendLine($">>Tổng quan:");
                strOutput.AppendLine($" - Số cp tăng giá: {Math.Round((float)lReport.Count(x => x.isPriceUp) * 100 / count, 1)}%");
                strOutput.AppendLine($" - Số cp trên MA20: {Math.Round((float)lReport.Count(x => x.isGEMA20) * 100 / count, 1)}%");

                var lTrenMa20 = lReport.Where(x => x.isPriceUp && x.isCrossMa20Up)
                                    .Take(20);
                var lAcceptFocus = lReport.Where(x => x.isSignalSell).Select(x => x.s);
                if (lTrenMa20.Any())
                {
                    strOutput.AppendLine();
                    strOutput.AppendLine($">>Top cp cắt lên MA20:");
                    var index = 1;
                    foreach (var item in lTrenMa20)
                    {
                        var content = $"{index++}. [{item.s}](https://fireant.vn/ma-chung-khoan/{item.s})";
                        if (item.isIchi)
                        {
                            content += " - Ichimoku";
                        }
                        strOutput.AppendLine(content);
                    }
                }

                if(isSave)
                {
                    _configRepo.InsertOne(new ConfigData
                    {
                        ty = (int)EConfigDataType.ChiBaoKyThuat,
                        t = t
                    });
                }

                //var lWyckoff = lReport.Where(x => x.Wyckoff != null);
                //var mesWyckoff = $">>Wyckoff:\n {string.Join("\n", lWyckoff.Select(x => $"+ {x.s}({x.Wyckoff.Date.ToString("dd/MM/yyyy")})"))}";

                return (1, strOutput.ToString(), PrintSignal(lReport), null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBaoKyThuat|EXCEPTION| {ex.Message}");
            }

            return (0, null, null, null);
        }

        public async Task<Stream> Chart_ThongKeKhopLenh(string sym = "10")
        {
            try
            {
                var dt = DateTime.Now;
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                var mode = (int)EConfigDataType.ThongKeKhopLenh;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t >= t))
                        return null;

                    _configRepo.DeleteMany(filter);
                }

                var dat = await _marketDataService.Money24h_GetThongke(
sym);
                Thread.Sleep(200);
                if (dat.data.Count() < 10)
                    return null;

                var first = dat.data.First();
                var dt_detect = first.trading_date.UnixTimeStampToDateTime();
                var t_detect = long.Parse($"{dt_detect.Year}{dt_detect.Month.To2Digit()}{dt_detect.Day.To2Digit()}");
                if (lConfig.Any(x => x.t >= t_detect))
                    return null;

                _configRepo.InsertOne(new ConfigData
                {
                    ty = mode,
                    t = t
                });

                return await _chartService.Chart_ThongKeKhopLenh(sym, dat);
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.Chart_ThongKeKhopLenh|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public bool Chart_4U()
        {
            try
            {
                var dt = DateTime.Now;
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                var lKhopLenh = _configRepo.GetByFilter(Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.ThongKeKhopLenh));
                if (!lKhopLenh.Any())
                    return false;

                var mode = (int)EConfigDataType.ForUser;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t >= t))
                        return false;

                    _configRepo.DeleteMany(filter);
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = mode,
                    t = t
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.Chart_4U|EXCEPTION| {ex.Message}");
            }
            return false;
        }

        private async Task<ReportPTKT> ChiBaoKyThuatOnlyStock(string code, int limitvol)
        {
            try
            {
                var lData = await _marketDataService.SSI_GetDataStock(code);
                if (lData.Count() < 250)
                    return null;

                var entity_Pivot = lData.Last();
                var val = entity_Pivot.Close * entity_Pivot.Volume;
                if (val < 100000)//Giá trị giao dịch < 100tr
                    return null;

                if (limitvol > 0 && lData.Last().Volume < limitvol)
                    return null;

                var model = new ReportPTKT
                {
                    s = code,
                };
                var lVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Close = x.Volume
                }).ToList();

                var lIchi = lData.GetIchimoku();
                var lBb = lData.GetBollingerBands();
                var lRsi = lData.GetRsi();
                var lMa20Vol = lVol.GetSma(20);

                //MA20
                var bb_Pivot = lBb.Last();
                var entity_Sig = lData.SkipLast(1).Last();
                var bb_Sig = lBb.SkipLast(1).Last();
                var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 1);
                var bandRate = Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value), 1);
                var ma20Vol = lMa20Vol.First(x => x.Date == entity_Sig.Date);


                //model.isFocus = rateVol <= (decimal)0.6;
                model.isGEMA20 = entity_Pivot.Close >= (decimal)bb_Pivot.Sma;
                model.isCrossMa20Up = entity_Sig.Close < (decimal)bb_Sig.Sma && entity_Pivot.Close >= (decimal)bb_Pivot.Sma && entity_Pivot.Open <= (decimal)bb_Pivot.Sma;
                model.isPriceUp = entity_Pivot.Close > entity_Pivot.Open;

                //Ichi
                var ichiCheck = lIchi.Last();
                if (entity_Pivot.Close > ichiCheck.SenkouSpanA && entity_Pivot.Close > ichiCheck.SenkouSpanB)
                {
                    model.isIchi = true;
                }

                //Focus: 
                /*
                    + Vol giảm một nửa
                    + BB band > 7%
                    + Vol Sig > Ma20 Vol 
                    + Pivot > Ma20
                    + Thuộc 1/3 so với Upper 
                 */
                if(bandRate > 7
                    && rateVol <= (decimal)0.6
                    && entity_Sig.Volume > (decimal)ma20Vol.Sma.Value
                    && entity_Pivot.Close > (decimal)bb_Pivot.Sma.Value)
                {
                    var max = Math.Max(entity_Pivot.Open, entity_Pivot.Close);
                    var check1_3 = (max - (decimal)bb_Pivot.Sma.Value) >= 2 * ((decimal)bb_Pivot.UpperBand.Value - max); 
                    if(check1_3)
                    {
                        model.isSignalSell = true;
                    }
                }
                //Foreign BUY/SELL
                var now = DateTime.Now;
                if(code != "VNINDEX")
                {
                    var info = await _marketDataService.SSI_GetStockInfo_Extend(code, now.AddYears(-1), now);
                    var res = model.IsForeign(lData, info.data);
                    if (res == EOrderType.BUY)
                    {
                        model.isForeignBuy = true;
                    }
                    else if (res == EOrderType.SELL)
                    {
                        model.isForeignSell = true;
                    }
                }

                //var wyckoff = lData.IsWyckoff();
                //if(wyckoff.Item1)
                //{
                //    model.Wyckoff = wyckoff.Item2.Last();
                //}
               
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBaoKyThuatOnlyStock|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private string PrintSignal(List<ReportPTKT> lInput)
        {
            var lFilter = lInput.Where(x => x.isSignalSell || x.isForeignBuy || x.isForeignSell)
                                .OrderByDescending(x => x.isForeignBuy)
                                .ThenByDescending(x => x.isForeignSell)
                                .ThenByDescending(x => x.isSignalSell);
            if(!lFilter.Any())
                return string.Empty;

            var sBuilder = new StringBuilder();
            sBuilder.AppendLine($"*Tín hiệu mua bán ngày: {DateTime.Now.ToString("dd/MM/yyyy")}*");
            foreach (var item in lFilter)
            {
                var mes = $"{item.s} => ";
                var lSig = new List<string>();
                if (item.isForeignBuy)
                {
                    lSig.Add("foreign MUA");
                }
                if (item.isForeignSell)
                {
                    lSig.Add("foreign BÁN");
                }
                if (item.isSignalSell)
                {
                    lSig.Add("signal BÁN");
                }
                mes += string.Join("+", lSig.ToArray());
                sBuilder.AppendLine(mes);
            }
            return sBuilder.ToString();
        }

        public async Task<(int, string)> AnalyzeSectorIndex()
        {
            try
            {
                var strOutput = new StringBuilder();
                strOutput.AppendLine("*PHÂN TÍCH SỨC MẠNH DÒNG TIỀN NGÀNH*");
                strOutput.AppendLine($"> Ngày: {DateTime.Now:dd/MM/yyyy}");
                strOutput.AppendLine();

                var lSectorRank = new List<(string Name, decimal Change, decimal RSI, bool IsAboveMA20, bool IsWyckoff)>();

                foreach (var sector in StaticVal._dicSectorIndex)
                {
                    try
                    {
                        var lData = await _marketDataService.Vietstock_GetDataStock(sector.Value);
                        if (lData == null || lData.Count < 50) continue;

                        var current = lData.Last();
                        var prev = lData.SkipLast(1).Last();
                        
                        // % Thay đổi
                        var change = Math.Round((current.Close - prev.Close) * 100 / prev.Close, 2);
                        
                        // RSI
                        var rsiList = lData.GetRsi(14);
                        var rsi = rsiList.Last()?.Rsi ?? 0;

                        // MA20
                        var ma20List = lData.GetSma(20);
                        var ma20 = ma20List.Last()?.Sma ?? 0;
                        var isAboveMA20 = current.Close >= (decimal)ma20;

                        // Wyckoff
                        var (isWyckoff, _) = lData.IsWyckoff();

                        // Pressure

                        var lDataT = lData.Select(x => new QuoteT
                        {
                            Date = x.Date,
                            Open = x.Open,
                            High = x.High,
                            Low = x.Low,
                            Close = x.Close,
                            Volume = x.Volume,
                            TimeStamp = new DateTimeOffset(x.Date).ToUnixTimeSeconds()
                        }).ToList();

                        lSectorRank.Add((sector.Key, (decimal)change, (decimal)rsi, isAboveMA20, isWyckoff));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AnalyzeSectorIndex error for {sector.Key}: {ex.Message}");
                    }
                }

                if (!lSectorRank.Any()) return (0, null);

                // Sắp xếp theo % Thay đổi
                var sorted = lSectorRank.OrderByDescending(x => x.Change).ToList();

                strOutput.AppendLine(">> Xếp hạng theo % thay đổi:");
                foreach (var item in sorted)
                {
                    var status = item.IsAboveMA20 ? "🚀" : "☁️";
                    var signals = new List<string>();
                    if (item.IsWyckoff) signals.Add("Wyckoff");
                    var signalStr = signals.Any() ? $" - *{string.Join(", ", signals)}*" : "";
                    
                    strOutput.AppendLine($"{status} *{item.Name}*: {item.Change}% | RSI: {Math.Round(item.RSI, 1)}{signalStr}");
                }

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.AnalyzeSectorIndex|EXCEPTION| {ex.Message}");
            }
            return (0, null);
        }
    }

    public static class clsExtend
    {
        public static EOrderType IsForeign(this ReportPTKT model, List<Quote> lData, List<SSI_DataStockInfoDetailResponse> lInfo)
        {
            try
            {
                lInfo.Reverse();
                var count = lInfo.Count;
                var sumNet = lInfo.Sum(x => Math.Abs(x.netBuySellVal ?? 0));
                var countNet = lInfo.Count(x => x.netBuySellVal != null && x.netBuySellVal != 0);
                var avg = sumNet / countNet;

                var lbb = lData.GetBollingerBands();

                var cur = lInfo.Last();
                var prev_1 = lInfo.SkipLast(1).Last();
                var prev_2 = lInfo.SkipLast(2).Last();
                var prev_3 = lInfo.SkipLast(3).Last();

                //CASE 1
                if (Math.Abs(prev_1.netBuySellVal ?? 0) > avg) 
                {
                    var prev = prev_2;
                    var sig = prev_1;
                    var pivot_1 = cur;

                    var check = isCheck(prev, sig, pivot_1, null);
                    if(check)
                    {
                        //
                        return Detect(prev, sig, pivot_1);
                    }
                }

                if (Math.Abs(prev_2.netBuySellVal ?? 0) > avg)
                {
                    var prev = prev_3;
                    var sig = prev_2;
                    var pivot_1 = prev_1;
                    var pivot_2 = cur;

                    var check = isCheck(prev, sig, pivot_1, pivot_2);
                    if (check)
                    {
                        //
                        return Detect(prev, sig, pivot_1);
                    }
                }

                bool isCheck(SSI_DataStockInfoDetailResponse prev, SSI_DataStockInfoDetailResponse sig, SSI_DataStockInfoDetailResponse pivot_1, SSI_DataStockInfoDetailResponse pivot_2)
                {
                    var ratePrev = Math.Round(-1 + Math.Abs((sig.netBuySellVal ?? 0) / (prev.netBuySellVal ?? 1)), 1);
                    var ratePivot = Math.Round(-1 + Math.Abs((sig.netBuySellVal ?? 0) / (pivot_1.netBuySellVal ?? 1)), 1);

                    if (Math.Abs(ratePrev) < 1.5 
                        || Math.Abs(ratePivot) < 1.5
                        || Math.Round((decimal)(prev.netBuySellVal ?? 0) / 1000000) == 0
                        || Math.Round((decimal)(sig.netBuySellVal ?? 0) / 1000000000, 1) == 0)
                    {
                        return false;
                    }

                    var checkMinVal = ((sig.netBuySellVal > 100000000) || (pivot_1.netBuySellVal > 100000000) || ((pivot_2?.netBuySellVal ?? 100000000) > 0));
                    if (!checkMinVal)
                        return false;

                    var dtPrev = prev.tradingDate.ToDateTime("dd/MM/yyyy");
                    var dtSig = sig.tradingDate.ToDateTime("dd/MM/yyyy");
                    var dtPivot = pivot_1.tradingDate.ToDateTime("dd/MM/yyyy");
                    var entityPrev = lData.First(x => x.Date.Day == dtPrev.Day && x.Date.Month == dtPrev.Month && x.Date.Year == dtPrev.Year);
                    var entitySignal = lData.First(x => x.Date.Day == dtSig.Day && x.Date.Month == dtSig.Month && x.Date.Year == dtSig.Year);
                    var entityPivot = lData.First(x => x.Date.Day == dtPivot.Day && x.Date.Month == dtPivot.Month && x.Date.Year == dtPivot.Year);
                    var bb_Signal = lbb.First(x => x.Date == entitySignal.Date);
                    var bb_Pivot = lbb.First(x => x.Date == entityPivot.Date);
                    if (entitySignal.Low < (decimal)bb_Signal.LowerBand.Value)
                    {
                        if (entitySignal.Low < entityPivot.Low)
                            return false;
                    }
                    else if (sig.netBuySellVal < 0
                        && entityPivot.Close > (decimal)bb_Pivot.Sma.Value)
                    {
                        var divUp = (decimal)bb_Pivot.UpperBand.Value - entityPivot.Close;
                        var divMA = entityPivot.Close - (decimal)bb_Pivot.Sma.Value;
                        if (divUp < 2 * divMA)
                            return false;
                    }

                    return true;
                }
                EOrderType Detect(SSI_DataStockInfoDetailResponse prev, SSI_DataStockInfoDetailResponse sig, SSI_DataStockInfoDetailResponse pivot_1)
                {
                    var dtPrev = prev.tradingDate.ToDateTime("dd/MM/yyyy");
                    var dtSig = sig.tradingDate.ToDateTime("dd/MM/yyyy");
                    var dtPivot = pivot_1.tradingDate.ToDateTime("dd/MM/yyyy");
                    var entityPrev = lData.First(x => x.Date.Day == dtPrev.Day && x.Date.Month == dtPrev.Month && x.Date.Year == dtPrev.Year);
                    var entitySignal = lData.First(x => x.Date.Day == dtSig.Day && x.Date.Month == dtSig.Month && x.Date.Year == dtSig.Year);
                    var entityPivot = lData.First(x => x.Date.Day == dtPivot.Day && x.Date.Month == dtPivot.Month && x.Date.Year == dtPivot.Year);

                    var bb_Signal = lbb.First(x => x.Date == entitySignal.Date);
                    var bb_Pivot = lbb.First(x => x.Date == entityPivot.Date);
                    if (entitySignal.High > (decimal)bb_Signal.UpperBand.Value)
                    {
                        //SELL
                        return EOrderType.SELL;
                    }
                    else if (sig.netBuySellVal < 0)
                    {
                        if (Math.Max(entitySignal.Open, entitySignal.Close) > (decimal)bb_Signal.Sma.Value
                            && entityPivot.Close < (decimal)bb_Pivot.Sma.Value)
                            return EOrderType.NONE;

                        //BUY
                        return EOrderType.BUY;
                    }
                    else
                    {
                        //SELL
                        return EOrderType.SELL;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"clsExtend.IsForeign|EXCEPTION| {ex.Message}");
            }

            return EOrderType.NONE;
        }
    }
}
