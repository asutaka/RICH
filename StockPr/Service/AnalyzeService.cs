using MongoDB.Driver;
using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System;
using System.Text;

namespace StockPr.Service
{
    public interface IAnalyzeService
    {
        Task<string> Realtime();
        Task<string> ThongKeGDNN_NhomNganh();
        Task<(int, string)> ChiBaoKyThuat(DateTime dt);
    }
    public class AnalyzeService : IAnalyzeService
    {
        private readonly ILogger<AnalyzeService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IStockRepo _stockRepo;
        private readonly ICategoryRepo _categoryRepo;
        private readonly IOrderBlockRepo _orderBlockRepo;
        public AnalyzeService(ILogger<AnalyzeService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo,
                                    IStockRepo stockRepo,
                                    ICategoryRepo categoryRepo,
                                    IOrderBlockRepo orderBlockRepo) 
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
            _stockRepo = stockRepo;
            _categoryRepo = categoryRepo;
            _orderBlockRepo = orderBlockRepo;
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

            //Chỉ báo OrderBlock
            try
            {
                var chibao = await CheckOrderBlock();
                if (chibao.Item1 > 0)
                {
                    sBuilder.AppendLine(chibao.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.Realtime|EXCEPTION(OrderBlock)| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<(int, string)> ChiBaoMA20()
        {
            try
            {
                var strOutput = new StringBuilder();

                var lMa = await _apiService.Money24h_GetMaTheoChiBao("ma20");
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
                    var lData = await _apiService.SSI_GetDataStock(item.s);
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
                    var content = $"{index}. {item.s}";
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

                var lMa = await _apiService.Money24h_GetMaTheoChiBao("break_1y");
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
                    var content = $"{index}. {item.s}";
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

        private async Task<(int, string)> CheckOrderBlock()
        {
            try
            {
                var strOutput = new StringBuilder();
                var lOB = _orderBlockRepo.GetAll();
                if (lOB.Any())
                {
                    var lStock = _stockRepo.GetAll();
                    var lSymbol = lOB.Select(x => x.s).Distinct();
                    lSymbol = lStock.Where(x => lSymbol.Any(y => y == x.s)).OrderBy(x => x.rank).Select(x => x.s).Take(50).ToList();
                    var lRes = new List<OrderBlock>();
                    foreach (var item in lSymbol)
                    {
                        var lData = await _apiService.SSI_GetDataStock(item);
                        var last = lData.LastOrDefault();
                        if(last != null)
                        {
                            var res = last.IsOrderBlock(lOB.Where(x => x.s == item));
                            if(res.Item1)
                            {
                                lRes.Add(res.Item2);
                            }
                        }

                        Thread.Sleep(100);
                    }

                    var lOrderBlockBuy = lRes.Where(x => x.Mode == (int)EOrderBlockMode.BotPinbar || x.Mode == (int)EOrderBlockMode.BotInsideBar);
                    if (lOrderBlockBuy.Any())
                    {
                        strOutput.AppendLine();
                        strOutput.AppendLine($"[Thông báo] Tín hiệu OrderBlock MUA");
                        var index = 1;
                        foreach (var item in lOrderBlockBuy)
                        {
                            var content = $"{index++}. {item.s}|ENTRY: {Math.Round(item.Entry, 1)}|SL: {Math.Round(item.SL, 1)}";
                            strOutput.AppendLine(content);
                        }
                    }

                    var lOrderBlockSell = lRes.Where(x => x.Mode == (int)EOrderBlockMode.TopPinbar || x.Mode == (int)EOrderBlockMode.TopInsideBar);
                    if (lOrderBlockSell.Any())
                    {
                        strOutput.AppendLine();
                        strOutput.AppendLine($"[Thông báo] Tín hiệu OrderBlock BÁN");
                        var index = 1;
                        foreach (var item in lOrderBlockSell)
                        {
                            var content = $"{index++}. {item.s}|ENTRY: {Math.Round(item.Entry, 1)}|SL: {Math.Round(item.SL, 1)}";
                            strOutput.AppendLine(content);
                        }
                    }
                }


                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.CheckOrderBlock|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        public async Task<string> ThongKeGDNN_NhomNganh()
        {
            var dt = DateTime.Now;
            var sBuilder = new StringBuilder();
            var foreign = await ThongkeForeign(dt);
            if(foreign.Item1 > 0)
            {
                sBuilder.AppendLine(foreign.Item2);
                sBuilder.AppendLine();
            }

            var nhomnganh = await ThongkeNhomNganh(dt);
            if(nhomnganh.Item1 > 0)
            {
                sBuilder.AppendLine(nhomnganh.Item2);
            }
            return sBuilder.ToString();
        }

        private async Task<(int, string)> ThongkeForeign(DateTime dt)
        {
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var type = EMoney24hTimeType.today;
                var mode = EConfigDataType.GDNN_today;
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
                var lData = new List<Money24h_ForeignResponse>();
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HSX, type));
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HNX, type));
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.UPCOM, type));
                if (!lData.Any())
                    return (0, null);

                var head = $"[Thông báo] GDNN ngày {dt.ToString("dd/MM/yyyy")}:"; ;
                strOutput.AppendLine(head);
                strOutput.AppendLine($"*Top mua ròng:");
                var lTopBuy = lData.OrderByDescending(x => x.net_val).Take(10);
                var lTopSell = lData.OrderBy(x => x.net_val).Take(10);
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    var content = $"{index}. {item.s} (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.00")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($"*Top bán ròng:");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. {item.s} (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.00")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)EConfigDataType.GDNN_today,
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

                var res = await _apiService.Money24h_GetNhomNganh(type);
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


                var head = $"[Thông báo] Nhóm ngành được quan tâm ngày {dt.ToString("dd/MM/yyyy")}:";
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

        public async Task<(int, string)> ChiBaoKyThuat(DateTime dt)
        {
            try
            {
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                FilterDefinition<ConfigData> filterConfig = Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.ChiBaoKyThuat);
                var lConfig = _configRepo.GetByFilter(filterConfig);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filterConfig);
                }

                var strOutput = new StringBuilder();
                var lReport = new List<ReportPTKT>();
                var filter = Builders<Stock>.Filter.Gte(x => x.status, 0);
                //xóa toàn bộ OrderBlock
                _orderBlockRepo.DeleteMany(Builders<OrderBlock>.Filter.Gte(x => x.Open, 0));
                var lStock = _stockRepo.GetByFilter(filter).OrderBy(x => x.rank);
                foreach (var item in lStock)
                {
                    try
                    {
                        var model = await ChiBaoKyThuatOnlyStock(item.s, 50000);
                        if (model is null)
                            continue;
                        model.rank = item.rank;

                        lReport.Add(model);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"CalculateService.ChiBaoKyThuat|EXCEPTION(ChiBaoKyThuatOnlyStock)| {ex.Message}");
                    }
                }

                var count = lReport.Count;
                //Tỉ lệ cp trên ma20 
                strOutput.AppendLine($"[Thống kê PTKT]");
                strOutput.AppendLine($"*Tổng quan:");
                strOutput.AppendLine($" - Số cp tăng giá: {Math.Round((float)lReport.Count(x => x.isPriceUp) * 100 / count, 1)}%");
                strOutput.AppendLine($" - Số cp trên MA20: {Math.Round((float)lReport.Count(x => x.isGEMA20) * 100 / count, 1)}%");

                var lTrenMa20 = lReport.Where(x => x.isPriceUp && x.isCrossMa20Up)
                                    .OrderBy(x => x.rank)
                                    .Take(20);
                if (lTrenMa20.Any())
                {
                    strOutput.AppendLine();
                    strOutput.AppendLine($"*Top cp cắt lên MA20:");
                    var index = 1;
                    foreach (var item in lTrenMa20)
                    {
                        var content = $"{index++}. {item.s}";
                        if (item.isIchi)
                        {
                            content += " - Ichimoku";
                        }
                        strOutput.AppendLine(content);
                    }
                }
                //
                var lOrderBlockBuy = lReport.Where(x => x.ob != null && (x.ob.Mode == (int)EOrderBlockMode.BotPinbar || x.ob.Mode == (int)EOrderBlockMode.BotInsideBar))
                                  .OrderBy(x => x.rank)
                                  .Take(10);
                if(lOrderBlockBuy.Any())
                {
                    strOutput.AppendLine();
                    strOutput.AppendLine($"*OrderBlock MUA:");
                    var index = 1;
                    foreach (var item in lOrderBlockBuy)
                    {
                        var content = $"{index++}. {item.s}|ENTRY: {Math.Round(item.ob.Entry, 1)}|SL: {Math.Round(item.ob.SL, 1)}";
                        strOutput.AppendLine(content);
                    }
                }

                var lOrderBlockSell = lReport.Where(x => x.ob != null && (x.ob.Mode == (int)EOrderBlockMode.TopPinbar || x.ob.Mode == (int)EOrderBlockMode.TopInsideBar))
                                 .OrderBy(x => x.rank)
                                 .Take(10);
                if (lOrderBlockSell.Any())
                {
                    strOutput.AppendLine();
                    strOutput.AppendLine($"*OrderBlock BÁN:");
                    var index = 1;
                    foreach (var item in lOrderBlockSell)
                    {
                        var content = $"{index++}. {item.s}|ENTRY: {Math.Round(item.ob.Entry, 1)}|SL: {Math.Round(item.ob.SL,1)}";
                        strOutput.AppendLine(content);
                    }
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)EConfigDataType.ChiBaoKyThuat,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBaoKyThuat|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<ReportPTKT> ChiBaoKyThuatOnlyStock(string code, int limitvol)
        {
            try
            {
                var lData = await _apiService.SSI_GetDataStock(code);

                #region Order Block
                var lOrderBlock = lData.GetOrderBlock(5);
                if (lOrderBlock.Any())
                {
                    foreach (var item in lOrderBlock)
                    {
                        item.s = code;
                        _orderBlockRepo.InsertOne(item);
                    }
                }
                #endregion

                var res = ChiBaoKyThuatOnlyStock(lData, limitvol);
                if (res != null)
                {
                    res.s = code;
                    var checkOB = lData.Last().IsOrderBlock(lOrderBlock);
                    if(checkOB.Item1)
                        res.ob = checkOB.Item2;

                    return res;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"CalculateService.ChiBaoKyThuatOnlyStock|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private ReportPTKT ChiBaoKyThuatOnlyStock(List<Quote> lData, int limitvol)
        {
            try
            {
                if (lData.Count() < 250)
                    return null;
                if (limitvol > 0 && lData.Last().Volume < limitvol)
                    return null;

                var model = new ReportPTKT();

                var lIchi = lData.GetIchimoku();
                var lBb = lData.GetBollingerBands();
                //var lEma21 = lData.GetEma(21);
                //var lEma50 = lData.GetEma(50);
                //var lEma200 = lData.GetEma(200);
                var lRsi = lData.GetRsi();

                //MA20
                var entity = lData.Last();
                var bb = lBb.Last();
                var entityNear = lData.SkipLast(1).TakeLast(1).First();
                var bbNear = lBb.SkipLast(1).TakeLast(1).First();

                model.isGEMA20 = entity.Close >= (decimal)bb.Sma;
                model.isCrossMa20Up = entityNear.Close < (decimal)bbNear.Sma && entity.Close >= (decimal)bb.Sma && entity.Open <= (decimal)bb.Sma;
                model.isPriceUp = entity.Close > entity.Open;

                //Ichi
                var ichiCheck = lIchi.Last();
                if (entity.Close > ichiCheck.SenkouSpanA && entity.Close > ichiCheck.SenkouSpanB)
                {
                    model.isIchi = true;
                }
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CalculateService.ChiBaoKyThuatOnlyStock|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
