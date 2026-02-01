using MongoDB.Driver;
using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IEPSRankService
    {
        Task<(int, string)> RankEPS(DateTime dt);
        Task<(decimal, decimal, decimal, decimal, decimal)> FreeFloat(string s);
        Task<Dictionary<string, Money24h_StatisticResponse>> RankMaCK(List<string> lInput);
        Task RankMaCKSync();
        Task<List<string>> ListFocus();
    }
    public class EPSRankService : IEPSRankService
    {
        private readonly ILogger<EPSRankService> _logger;
        private readonly IMarketDataService _marketDataService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IPhanLoaiNDTRepo _phanloaiRepo;
        public EPSRankService(ILogger<EPSRankService> logger,
                                    IMarketDataService marketDataService,
                                    IConfigDataRepo configRepo,
                                    IPhanLoaiNDTRepo phanloaiRepo)
        {
            _logger = logger;
            _marketDataService = marketDataService;
            _configRepo = configRepo;
            _phanloaiRepo = phanloaiRepo;
        }
        /// <summary>
        /// FreeFloat + EPS  + PE + Nợ/Vốn chủ
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public async Task<(decimal, decimal, decimal, decimal, decimal)> FreeFloat(string s)
        {
            try
            {
                var finance = await _marketDataService.SSI_GetFinanceStock(s);
                if (finance is null)
                    return (0, 0, 0, 0, 0);
                if (finance.eps is null)
                    finance.eps = 0;
                if(finance.pe is null)
                    finance.pe = 0;
                if (finance.pb is null)
                    finance.pb = 0;
                if (finance.debtEquity is null)
                    finance.debtEquity = 0; 

                var freefloat = await _marketDataService.SSI_GetFreefloatStock(s);
                return (freefloat, finance.eps.Value, finance.pe.Value, finance.pb.Value, finance.debtEquity.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"EPSRankService.FreeFloat|EXCEPTION| {ex.Message}");
            }
            return (0, 0, 0, 0, 0);
        }
        public async Task<(int, string)> RankEPS(DateTime dt)
        {
            try
            {
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
                FilterDefinition<ConfigData> filterConfig = Builders<ConfigData>.Filter.Eq(x => x.ty, (int)EConfigDataType.EPS);
                var lConfig = _configRepo.GetByFilter(filterConfig);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filterConfig);
                }

                var strOutput = new StringBuilder();
                strOutput.AppendLine("[Thống kê EPS]");
                var lEPS = new List<(string, decimal)>();
                foreach (var item in StaticVal._lStock)
                {
                    try
                    {
                        var finance = await _marketDataService.SSI_GetFinanceStock(item.s);
                        if (finance is null)
                            continue;

                        if(finance.eps >= 5000)
                        {
                            lEPS.Add((item.s, Math.Round(finance.eps.Value)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AnalyzeService.RankEPS|EXCEPTION(RankEPS)| {ex.Message}");
                    }
                }
                lEPS = lEPS.OrderByDescending(x => x.Item2).Take(100).ToList();
                var index = 1;
                foreach (var item in lEPS)
                {
                    string mes = string.Empty;
                    var freefloat = await _marketDataService.SSI_GetFreefloatStock(item.Item1);
                    if(freefloat <= 10)
                    {
                        mes = $"{index++}. [{item.Item1}](https://finance.vietstock.vn/{item.Item1}/phan-tich-ky-thuat.htm) (eps: {item.Item2.ToString("#,##0")})";
                    }
                    else
                    {
                        mes = $"{index++}. [{item.Item1}](https://finance.vietstock.vn/{item.Item1}/phan-tich-ky-thuat.htm) (eps: {item.Item2.ToString("#,##0")} - freefloat: {freefloat}%)";
                    }
                    strOutput.AppendLine(mes);
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)EConfigDataType.EPS,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"EPSRankService.RankEPS|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        public async Task<Dictionary<string, Money24h_StatisticResponse>> RankMaCK(List<string> lInput)
        {
            var dicOutput = new Dictionary<string, Money24h_StatisticResponse>();
            try
            {
                var dic = new Dictionary<string, Money24h_StatisticResponse>();
                foreach (var item in lInput)
                {
                    var dat = await _marketDataService.Money24h_GetThongke(item);
                    Thread.Sleep(200);
                    if (dat.data.Count() < 10)
                        continue;

                    dic.Add(item, dat);
                }
                var dicPoint = new Dictionary<string, int>();

                foreach (var item in dic)
                {
                    var ldat = item.Value.data.Take(20).ToList();
                    ldat.Reverse();
                    var TY = 1000000000;
                    var point = 0;

                    var lIndividual = ldat.Select(x => (x.local_individual_buy_matched - x.local_individual_sell_matched) / TY);
                    var lGroup = ldat.Select(x => (x.local_institutional_buy_matched - x.local_institutional_sell_matched) / TY);

                    //Last
                    var groupLast = lGroup.Last();
                    var individualLast = lIndividual.Last();

                    //Avg absolute
                    var individualAVG = lIndividual.Select(x => Math.Abs(x)).Average();
                    var groupAVG = lGroup.Select(x => Math.Abs(x)).Average();

                    //Trung bình 5 phiên trước
                    var individualAVG_5 = lIndividual.SkipLast(1).TakeLast(5).Select(x => Math.Abs(x)).Average();
                    var groupAVG_5 = lGroup.SkipLast(1).TakeLast(5).Select(x => Math.Abs(x)).Average();
                   
                    //Nếu Tổ chức bán ròng với vol > trung bình 5 phiên trước và > trung bình 
                    if(groupLast < 0
                        && Math.Abs(groupLast) > groupAVG_5
                        && Math.Abs(groupLast) > groupAVG)
                    {
                        point -= 500;
                    }

                    //Nếu Nhỏ lẻ mua ròng với vol > trung bình 5 phiên trước và > trung bình
                    if (individualLast > 0
                        && Math.Abs(individualLast) > individualAVG_5
                        && Math.Abs(individualLast) > individualAVG)
                    {
                        point -= 100;
                    }

                    //Nếu Tổ chức mua ròng 5/6 phiên gần nhất với vol > trung bình 
                    var isGroupPass_5 = lGroup.TakeLast(6).Count(x => x > 0) >= 5;
                    if(isGroupPass_5)
                    {
                        var avgGroupPass = lGroup.TakeLast(6).Average();
                        if(avgGroupPass > groupAVG)
                        {
                            point += 100;
                        }
                        point += 50;
                    }
                    //Nếu Tổ chức mua ròng 8/10 phiên gần nhất với vol > trung bình 
                    var isGroupPass_8 = lGroup.TakeLast(10).Count(x => x > 0) >= 8;
                    if (isGroupPass_8)
                    {
                        var avgGroupPass = lGroup.TakeLast(6).Average();
                        if (avgGroupPass > groupAVG)
                        {
                            point += 100;
                        }
                        point += 50;
                    }
                    //Nếu Tổ chức mua ròng 3/6 phiên gần nhất với vol > trung bình 
                    var isGroupPass_3 = lGroup.TakeLast(6).Count(x => x > 0) >= 3;
                    if (isGroupPass_3)
                    {
                        point += 50;
                    }
                    //5 phiên gần nhất Tổ chức không bán phiên nào vượt quá trung bình 
                    var GroupMin5 = lGroup.TakeLast(6).Min();
                    if(GroupMin5 < 0 
                        && Math.Abs(GroupMin5) > groupAVG)
                    {
                        point -= 200;
                    } 
                    
                    //Tổ chức mua ròng phiên gần nhất
                    if(groupLast > 0)
                    {
                        point += 25;
                    }
                    else
                    {
                        point -= 5;
                    }
                    //Nhỏ lẻ bán ròng phiên gần nhất
                    if(individualLast < 0)
                    {
                        point += 15;
                    }
                    else
                    {
                        point -= 5;
                    }

                    dicPoint.Add(item.Key, point);
                }

                foreach (var item in dicPoint.OrderByDescending(x => x.Value))
                {
                    var itemDic = dic.First(x => x.Key == item.Key);
                    dicOutput.Add(itemDic.Key, itemDic.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"EPSRankService.RankMaCK|EXCEPTION| {ex.Message}");
            }

            return dicOutput;
        }

        public async Task RankMaCKSync()
        {
            try
            {
                var vnindex = await GetThongKe("10");
                Thread.Sleep(100);
                if (vnindex.Item1 != null)
                {
                    var entityVNINDEX = _phanloaiRepo.GetEntityByFilter(Builders<PhanLoaiNDT>.Filter.Eq(x => x.s, "VNINDEX"));
                    if (entityVNINDEX is null)
                    {
                        _phanloaiRepo.InsertOne(new PhanLoaiNDT
                        {
                            s = "VNINDEX",
                            Date = vnindex.Item1,
                            Foreign = vnindex.Item2,
                            TuDoanh = vnindex.Item3,
                            Individual = vnindex.Item4,
                            Group = vnindex.Item5,
                        });
                    }
                    else
                    {
                        var take = vnindex.Item1.Count(x => x > entityVNINDEX.Date.Last());
                        if (take > 0)
                        {
                            entityVNINDEX.Date.AddRange(vnindex.Item1.TakeLast(take));
                            entityVNINDEX.Foreign.AddRange(vnindex.Item2.TakeLast(take));
                            entityVNINDEX.TuDoanh.AddRange(vnindex.Item3.TakeLast(take));
                            entityVNINDEX.Individual.AddRange(vnindex.Item4.TakeLast(take));
                            entityVNINDEX.Group.AddRange(vnindex.Item5.TakeLast(take));
                            _phanloaiRepo.Update(entityVNINDEX);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"EPSRankService.RankMaCKSync|EXCEPTION| {ex.Message}");
            }

            foreach (var item in StaticVal._lStock)
            {
                try
                {
                    var dat = await GetThongKe(item.s);
                    Thread.Sleep(100);
                    if (dat.Item1 != null)
                    {
                        var entityVNINDEX = _phanloaiRepo.GetEntityByFilter(Builders<PhanLoaiNDT>.Filter.Eq(x => x.s, item.s));
                        if (entityVNINDEX is null)
                        {
                            _phanloaiRepo.InsertOne(new PhanLoaiNDT
                            {
                                s = item.s,
                                Date = dat.Item1,
                                Foreign = dat.Item2,
                                TuDoanh = dat.Item3,
                                Individual = dat.Item4,
                                Group = dat.Item5,
                            });
                        }
                        else
                        {
                            var take = dat.Item1.Count(x => x > entityVNINDEX.Date.Last());
                            if (take > 0)
                            {
                                entityVNINDEX.Date.AddRange(dat.Item1.TakeLast(take));
                                entityVNINDEX.Foreign.AddRange(dat.Item2.TakeLast(take));
                                entityVNINDEX.TuDoanh.AddRange(dat.Item3.TakeLast(take));
                                entityVNINDEX.Individual.AddRange(dat.Item4.TakeLast(take));
                                entityVNINDEX.Group.AddRange(dat.Item5.TakeLast(take));
                                _phanloaiRepo.Update(entityVNINDEX);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EPSRankService.RankMaCKSync|EXCEPTION| {ex.Message}");
                }
            }
        }

        public async Task<List<string>> ListFocus()
        {
            var lfocus = new List<string>();
            foreach (var item in StaticVal._lStock.Where(x => x.ex == (int)EExchange.HSX))
            {
                try
                {
                    if (item.s == "VNINDEX")
                        continue;

                    var entityVNINDEX = _phanloaiRepo.GetEntityByFilter(Builders<PhanLoaiNDT>.Filter.Eq(x => x.s, item.s));
                    if (entityVNINDEX is null || entityVNINDEX.Date.Count() < 30)
                        continue;
                    var group = entityVNINDEX.Group.TakeLast(50);

                    var group_COUNT = group.Count();
                    var group_COUNT_INCREASE = group.Count(x => x > 0);
                    if (((double)group_COUNT_INCREASE / group_COUNT) < 0.5) 
                        continue;

                    var group_INCREASE = group.Where(x => x > 0).Sum();
                    var group_DECREASE = group.Where(x => x < 0).Sum();
                    if (group_INCREASE < 100
                        || Math.Abs(group_DECREASE) * 2 > group_INCREASE) 
                        continue;

                    var individual_INCREASE = entityVNINDEX.Individual.TakeLast(50).Where(x => x > 0).Sum();
                    var foreign_INCREASE = entityVNINDEX.Foreign.TakeLast(50).Where(x => x > 0).Sum();
                    var tudoanh_INCREASE = entityVNINDEX.TuDoanh.TakeLast(50).Where(x => x > 0).Sum();
                    var rate = group_INCREASE / (individual_INCREASE + foreign_INCREASE + tudoanh_INCREASE);
                    if (rate < 0.5)
                        continue;

                    var group_DECREASE_15 = entityVNINDEX.Group.TakeLast(15).Where(x => x < 0);
                    var foreign_DECREASE_15 = entityVNINDEX.Foreign.TakeLast(15).Where(x => x < 0);
                    var individual_DECREASE_15 = entityVNINDEX.Individual.TakeLast(15).Where(x => x < 0);
                    var tudoanh_DECREASE_15 = entityVNINDEX.TuDoanh.TakeLast(15).Where(x => x < 0);

                    var group_DECREASE_15_MIN = group_DECREASE_15.Any() ? group_DECREASE_15.Min() : 0;
                    var foreign_DECREASE_15_MIN = foreign_DECREASE_15.Any() ? foreign_DECREASE_15.Min() : 0;
                    var individual_DECREASE_15_MIN = individual_DECREASE_15.Any() ? individual_DECREASE_15.Min() : 0;
                    var tudoanh_DECREASE_15_MIN = tudoanh_DECREASE_15.Any() ? tudoanh_DECREASE_15.Min() : 0;

                    var min = Math.Min(tudoanh_DECREASE_15_MIN, Math.Min(foreign_DECREASE_15_MIN, individual_DECREASE_15_MIN));

                    if (group_DECREASE_15_MIN < min)
                        continue;

                    var lData = await _marketDataService.SSI_GetDataStock(item.s);
                    if (lData.Count() < 250
                        || lData.Last().Volume < 50000)
                        continue;

                    var lbb = lData.GetBollingerBands();
                    var bb = lbb.Last();
                    var cur = lData.Last();
                    var isValid = cur.Close > (decimal)bb.UpperBand
                                    || cur.Close < (decimal)bb.Sma
                                    || ((decimal)bb.UpperBand - cur.Close) < (cur.Close - (decimal)bb.Sma);
                    if (isValid)
                        continue;

                    lfocus.Add(item.s);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EPSRankService.RankMaCKSync|EXCEPTION| {ex.Message}");
                }
            }

            return lfocus;
        }

        private async Task<(List<double>, List<double>, List<double>, List<double>, List<double>)> GetThongKe(string sym)
        {
            try
            {
                var dat = await _marketDataService.Money24h_GetThongke(sym);
                if (dat is null
                    || !dat.data.Any())
                    return (null, null, null, null, null);
                var TY = 1000000000;

                dat.data = dat.data.Take(20).ToList();
                dat.data.Reverse();

                var lCat = dat.data.Select(x => (double)x.trading_date).ToList();
                var lForeign = dat.data.Select(x => (x.foreign_buy_matched - x.foreign_sell_matched) / TY).ToList();
                var lTudoanh = dat.data.Select(x => (x.proprietary_buy_matched - x.proprietary_sell_matched) / TY).ToList();
                var lIndividual = dat.data.Select(x => (x.local_individual_buy_matched - x.local_individual_sell_matched) / TY).ToList();
                var lGroup = dat.data.Select(x => (x.local_institutional_buy_matched - x.local_institutional_sell_matched) / TY).ToList();

                return (lCat, lForeign, lTudoanh, lIndividual, lGroup);
            }
            catch(Exception ex)
            {
                _logger.LogError($"EPSRankService.GetThongKe|EXCEPTION| {ex.Message}");
            }

            return (null, null, null, null, null);
        }
    }
}
