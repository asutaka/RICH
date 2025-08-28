using MongoDB.Driver;
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
    }
    public class EPSRankService : IEPSRankService
    {
        private readonly ILogger<EPSRankService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        public EPSRankService(ILogger<EPSRankService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
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
                var finance = await _apiService.SSI_GetFinanceStock(s);
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

                var freefloat = await _apiService.SSI_GetFreefloatStock(s);
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
                        var finance = await _apiService.SSI_GetFinanceStock(item.s);
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
                    var freefloat = await _apiService.SSI_GetFreefloatStock(item.Item1);
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
                    var dat = await _apiService.Money24h_GetThongke(item);
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
    }
}
