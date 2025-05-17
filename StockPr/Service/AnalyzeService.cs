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
        Task<(int, string, IEnumerable<string>)> ChiBaoKyThuat(DateTime dt);
        Task<(int, string)> ThongkeForeign_PhienSang(DateTime dt);
    }
    public class AnalyzeService : IAnalyzeService
    {
        private readonly ILogger<AnalyzeService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IStockRepo _stockRepo;
        private readonly ICategoryRepo _categoryRepo;
        public AnalyzeService(ILogger<AnalyzeService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo,
                                    IStockRepo stockRepo,
                                    ICategoryRepo categoryRepo) 
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
            _stockRepo = stockRepo;
            _categoryRepo = categoryRepo;
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm)";
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm)";
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
            }

            var week = await ThongkeTuDoanhWeek(dt);
            if (week.Item1 > 0)
            {
                sBuilder.AppendLine(week.Item2);
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
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HSX, type));
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HNX, type));
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.UPCOM, type));
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
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
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HSX, type));
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.#")} tỷ)";
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
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HSX, type));
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.HNX, type));
                lData.AddRange(await _apiService.Money24h_GetForeign(EExchange.UPCOM, type));
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Mua ròng {Math.Abs(item.net_val).ToString("#,##0.00")} tỷ)";
                    strOutput.AppendLine(content);
                    index++;
                }

                strOutput.AppendLine();
                strOutput.AppendLine($">>Top bán ròng");
                index = 1;
                foreach (var item in lTopSell)
                {
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Bán ròng {Math.Abs(item.net_val).ToString("#,##0.00")} tỷ)";
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
                lData.AddRange(await _apiService.Money24h_GetTuDoanh(EExchange.HSX, type));
                if (!lData.Any())
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Mua ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Bán ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
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
                lData.AddRange(await _apiService.Money24h_GetTuDoanh(EExchange.HSX, type));
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Mua ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
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
                    var content = $"{index}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Bán ròng {Math.Abs(item.prop_net).ToString("#,##0.#")} tỷ)";
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

        public async Task<(int, string, IEnumerable<string>)> ChiBaoKyThuat(DateTime dt)
        {
            try
            {
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                FilterDefinition<ConfigData> filterConfig = Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.ChiBaoKyThuat);
                var lConfig = _configRepo.GetByFilter(filterConfig);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null, null);

                    _configRepo.DeleteMany(filterConfig);
                }

                var strOutput = new StringBuilder();
                var lReport = new List<ReportPTKT>();
                //var filter = Builders<Stock>.Filter.Gte(x => x.status, 0);
                //var lStock = _stockRepo.GetByFilter(filter).OrderBy(x => x.rank);
                foreach (var item in StaticVal._lFocus)
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

                var lFocus = lReport.Where(x => StaticVal._lFocus.Contains(x.s));
                var lTrenMa20 = lFocus.Where(x => x.isPriceUp && x.isCrossMa20Up)
                                    .Take(20);
                var lAcceptFocus = lFocus.Where(x => x.isSignalSell).Select(x => x.s);
                if (lTrenMa20.Any())
                {
                    strOutput.AppendLine();
                    strOutput.AppendLine($">>Top cp cắt lên MA20:");
                    var index = 1;
                    foreach (var item in lTrenMa20)
                    {
                        var content = $"{index++}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm)";
                        if (item.isIchi)
                        {
                            content += " - Ichimoku";
                        }
                        strOutput.AppendLine(content);
                    }
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)EConfigDataType.ChiBaoKyThuat,
                    t = t
                });

                return (1, strOutput.ToString(), lAcceptFocus);
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBaoKyThuat|EXCEPTION| {ex.Message}");
            }

            return (0, null, null);
        }

        private async Task<ReportPTKT> ChiBaoKyThuatOnlyStock(string code, int limitvol)
        {
            try
            {
                var lData = await _apiService.SSI_GetDataStock(code);
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

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.ChiBaoKyThuatOnlyStock|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
