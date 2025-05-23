﻿using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IGiaNganhHangService
    {
        Task<(int, string)> TraceGia(bool isAll);
    }
    public class GiaNganhHangService : IGiaNganhHangService
    {
        private readonly ILogger<GiaNganhHangService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IMacroMicroRepo _macroRepo;
        private readonly int _flag = 7;
        public GiaNganhHangService(ILogger<GiaNganhHangService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo,
                                    IMacroMicroRepo macroRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
            _macroRepo = macroRepo;
        }

        private async Task<TraceGiaModel> Pig333_GiaThitHeo(bool isAll)
        {
            try
            {
                var dt = DateTime.Now;
                var pig = await _apiService.Pig333_GetPigPrice();
                var modelPig = new TraceGiaModel
                {
                    content = "Giá thịt heo",
                    description = "DBC,BAF,HAG",
                    link = "https://www.pig333.com/markets_and_prices/vietnam_166"
                };
                var isPrintPig = false;
                if (pig != null && pig.Any())
                {
                    try
                    {
                        var last = pig.Last();
                        modelPig.price = last.Value;
                        //weekly
                        var dtPrev = dt.AddDays(-2);
                        if (last.Date >= dtPrev)
                        {
                            var lastWeek = pig.SkipLast(1).Last();
                            var rateWeek = Math.Round(100 * (-1 + last.Value / lastWeek.Value), 1);
                            modelPig.weekly = rateWeek;
                            if (rateWeek >= _flag || rateWeek <= -_flag)
                            {
                                isPrintPig = true;
                            }
                        }
                        //Monthly
                        var dtMonthly = dt.AddMonths(-1);
                        var itemMonthly = pig.Where(x => x.Date <= dtMonthly).OrderByDescending(x => x.Date).FirstOrDefault();
                        if (itemMonthly != null)
                        {
                            var rateMonthly = Math.Round(100 * (-1 + last.Value / itemMonthly.Value), 1);
                            modelPig.monthly = rateMonthly;
                        }
                        //yearly
                        var dtYearly = dt.AddYears(-1);
                        var itemYearly = pig.Where(x => x.Date <= dtYearly).OrderByDescending(x => x.Date).FirstOrDefault();
                        if (itemYearly != null)
                        {
                            var rateYearly = Math.Round(100 * (-1 + last.Value / itemYearly.Value), 1);
                            modelPig.yearly = rateYearly;
                        }
                        //YTD
                        var dtYTD = new DateTime(dt.Year, 1, 2);
                        var itemYTD = pig.Where(x => x.Date <= dtYTD).OrderByDescending(x => x.Date).FirstOrDefault();
                        if (itemYTD != null)
                        {
                            var rateYTD = Math.Round(100 * (-1 + last.Value / itemYTD.Value), 1);
                            modelPig.YTD = rateYTD;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"GiaNganhHangService.TraceGia|EXCEPTION| {ex.Message}");
                    }
                }
                //Print
                if (isAll || isPrintPig)
                {
                    return modelPig;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.Pig333_GiaThitHeo|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<TraceGiaModel> MacroMicroLocal(bool isAll, string key)
        {
            try
            {
                var model = new TraceGiaModel();
                if (key.Equals("44756"))
                {
                    model.content = "Cước Container";
                    model.description = "HAH";
                    model.link = "https://www.drewry.co.uk/supply-chain-advisors/supply-chain-expertise/world-container-index-assessed-by-drewry";
                }
                else
                {
                    model.content = "Cước vận tải dầu thô";
                    model.description = "PVT,VTO";
                    model.link = "https://en.macromicro.me/charts/946/world-baltic-dirty-clean-tanker-index";
                }

                var builder = Builders<MacroMicro>.Filter;
                var filter = builder.Eq(x => x.key, key);
                var entity = _macroRepo.GetEntityByFilter(filter);
                if (entity is null)
                    return null;

                var isPrint = false;
                model.weekly = (decimal)entity.W;
                model.monthly = (decimal)entity.M;
                model.yearly = (decimal)entity.Y;
                model.YTD = (decimal)entity.YTD;
                model.price = (decimal)entity.price;
                if (model.weekly >= _flag || model.weekly <= -_flag)
                {
                    isPrint = true;
                }

                //Print
                if (isAll || isPrint)
                {
                    return model;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.MacroMicroLocal|EXCEPTION| {ex.Message}");
            }
            return null;
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
            catch(Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.MacroMicro|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<TraceGiaModel> Metal_GetYellowPhotpho(bool isAll)
        {
            try
            {
                var dt = DateTime.Now;
                var lPhotpho = await _apiService.Metal_GetYellowPhotpho();
                var modelPhotpho = new TraceGiaModel
                {
                    content = "Phốt pho vàng",
                    description = "DGC,PAT",
                    link = "https://www.sunsirs.com/uk/prodetail-708.html"
                };
                var isPrintPhotpho = false;
                if (lPhotpho?.Any() ?? false)
                {
                    try
                    {
                        foreach (var item in lPhotpho)
                        {
                            item.metalsPrice.Date = item.metalsPrice.renewDate.ToDateTime("yyyy-MM-dd");
                        }
                        var cur = lPhotpho.First();
                        modelPhotpho.price = cur.metalsPrice.average;
                        //weekly
                        var dtPrev = dt.AddDays(-2);
                        if (cur.metalsPrice.Date >= dtPrev)
                        {
                            var nearTime = cur.metalsPrice.Date.AddDays(-6);
                            var itemWeekly = lPhotpho.Where(x => x.metalsPrice.Date <= nearTime).OrderByDescending(x => x.metalsPrice.Date).First();
                            var rateWeek = Math.Round(100 * (-1 + cur.metalsPrice.average / itemWeekly.metalsPrice.average), 1);
                            modelPhotpho.weekly = rateWeek;
                            if (rateWeek >= _flag || rateWeek <= -_flag)
                            {
                                isPrintPhotpho = true;
                            }
                        }
                        //Monthly
                        var dtMonthly = dt.AddMonths(-1);
                        var itemMonthly = lPhotpho.Where(x => x.metalsPrice.Date <= dtMonthly).OrderByDescending(x => x.metalsPrice.Date).FirstOrDefault();
                        if (itemMonthly != null)
                        {
                            var rateMonthly = Math.Round(100 * (-1 + cur.metalsPrice.average / itemMonthly.metalsPrice.average), 1);
                            modelPhotpho.monthly = rateMonthly;
                        }
                        //yearly
                        var dtYearly = dt.AddYears(-1);
                        var itemYearly = lPhotpho.Where(x => x.metalsPrice.Date <= dtYearly).OrderByDescending(x => x.metalsPrice.Date).FirstOrDefault();
                        if (itemYearly != null)
                        {
                            var rateYearly = Math.Round(100 * (-1 + cur.metalsPrice.average / itemYearly.metalsPrice.average), 1);
                            modelPhotpho.yearly = rateYearly;
                        }
                        //YTD
                        var dtYTD = new DateTime(dt.Year, 1, 2);
                        var itemYTD = lPhotpho.Where(x => x.metalsPrice.Date <= dtYTD).OrderByDescending(x => x.metalsPrice.Date).FirstOrDefault();
                        if (itemYTD != null)
                        {
                            var rateYTD = Math.Round(100 * (-1 + cur.metalsPrice.average / itemYTD.metalsPrice.average), 1);
                            modelPhotpho.YTD = rateYTD;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"GiaNganhHangService.Metal_GetYellowPhotpho|EXCEPTION| {ex.Message}");
                    }
                }
                //Print
                if (isAll || isPrintPhotpho)
                {
                    return modelPhotpho;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.Metal_GetYellowPhotpho|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<List<TraceGiaModel>> Tradingeconimic_Commodities(bool isAll)
        {
            var lTraceGia = new List<TraceGiaModel>();
            try
            {
                var lEconomic = await _apiService.Tradingeconimic_Commodities();
                foreach (var item in lEconomic)
                {
                    if (isAll || item.Weekly >= _flag || item.Weekly <= -_flag)
                    {
                        if (item.Code.Equals(EPrice.Crude_Oil.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Dầu thô",
                                description = "PLX,OIL",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/crude-oil"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Natural_gas.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Khí tự nhiên",
                                description = "GAS,DCM,DPM",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/natural-gas"
                            });
                        }
                        else if (item.Code.Replace("-", "").Replace(" ", "").Equals(EPrice.kraftpulp.GetDisplayName().Replace("-","").Replace(" ", ""), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Bột giấy",
                                description = "DHC",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/kraft-pulp"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Coal.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Than",
                                description = "",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/coal"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Gold.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Vàng",
                                description = "",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/gold"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Steel.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Thép",
                                description = "HPG",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/steel"
                            });
                        }
                        else if (item.Code.Equals(EPrice.HRC_Steel.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "HRC",
                                description = "HSG,NKG,GDA",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/hrc-steel"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Rubber.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Cao su",
                                description = "TRC,DRI",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/rubber"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Coffee.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Cà phê",
                                description = "CTP",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/coffee"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Rice.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Gạo",
                                description = "LTG",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/rice"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Sugar.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Đường",
                                description = "SLS,LSS,SBT,QNS,CBS",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/sugar"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Urea.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "U rê",
                                description = "DPM,DCM,DHB",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/urea"
                            });
                        }
                        else if (item.Code.Equals(EPrice.DAP.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Phân bón DAP",
                                description = "DDV",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/di-ammonium"
                            });
                        }
                        else if (item.Code.Equals(EPrice.polyvinyl.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Hạt nhựa PVC",
                                description = "BMP,NTP",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/polyvinyl"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Nickel.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Niken",
                                description = "PC1",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/nickel"
                            });
                        }
                        else if (item.Code.Equals(EPrice.milk.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Sữa",
                                description = "VNM",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/milk"
                            });
                        }
                        else if (item.Code.Equals(EPrice.Cotton.GetDisplayName(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            lTraceGia.Add(new TraceGiaModel
                            {
                                content = "Bông",
                                description = "STK,ADS,HTG,PPH",
                                weekly = item.Weekly,
                                monthly = item.Monthly,
                                yearly = item.YoY,
                                YTD = item.YTD,
                                price = item.Price,
                                link = "https://tradingeconomics.com/commodity/cotton"
                            });
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.Tradingeconimic_Commodities|EXCEPTION| {ex.Message}");
            }

            return lTraceGia;
        }
        private string PrintTraceGia(TraceGiaModel model)
        {
            var res = $"   - [{model.content}]({model.link}): W({model.weekly}%)|M({model.monthly}%)|Y({model.yearly}%)|YTD({model.YTD}%)";
            if (!string.IsNullOrWhiteSpace(model.description))
            {
                var strSplit = model.description.Split(',');
                var lMes = new List<string>();
                foreach (var item in strSplit)
                {
                    var mes = $"[{item}](https://finance.vietstock.vn/{item}/phan-tich-ky-thuat.htm)";
                    lMes.Add(mes);
                }
                res += $"\n       => Giá hiện tại: {model.price}| {string.Join(", ", lMes)}\n";
            }
            return res;
        }
        public async Task<(int, string)> TraceGia(bool isAll)
        {
            var dt = DateTime.Now;
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            var dTime = new DateTimeOffset(new DateTime(dt.Year, dt.Month, dt.Day)).ToUnixTimeSeconds();
            try
            {
                var mode = EConfigDataType.TraceGia;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, (int)mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var lTraceGia = new List<TraceGiaModel>();
                var strOutput = new StringBuilder();

                //PIG
                var pig = await Pig333_GiaThitHeo(isAll);
                if (pig != null) 
                {
                    lTraceGia.Add(pig);
                }

                //WCI
                var wci = await MacroMicroLocal(isAll, "44756");
                if (wci != null)
                {
                    lTraceGia.Add(wci);
                }

                //Yellow PhotPho
                var yellowPhotpho = await Metal_GetYellowPhotpho(isAll);
                if (yellowPhotpho != null)
                {
                    lTraceGia.Add(yellowPhotpho);
                }

                //BDTI
                var bdti = await MacroMicroLocal(isAll, "946");
                if (bdti != null)
                {
                    lTraceGia.Add(bdti);
                }

                //Economic
                var lTradingEconomic = await Tradingeconimic_Commodities(isAll);
                if (lTradingEconomic?.Any() ?? false)
                {
                    lTraceGia.AddRange(lTradingEconomic);
                }

                foreach (var item in lTraceGia.OrderByDescending(x => x.weekly).ThenByDescending(x => x.monthly).ThenByDescending(x => x.yearly))
                {
                    strOutput.AppendLine(PrintTraceGia(item));
                }

                if (isAll)
                {
                    _configRepo.InsertOne(new ConfigData
                    {
                        ty = (int)mode,
                        t = t
                    });
                }

                if (strOutput.Length > 0)
                {
                    strOutput.Insert(0, "[Giá một số ngành hàng]\n");
                    return (1, strOutput.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GiaNganhHangService.TraceGia|EXCEPTION| {ex.Message}");
            }
            return (0, string.Empty);
        }
    }
}
