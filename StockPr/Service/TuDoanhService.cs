using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface ITuDoanhService
    {
        Task<(int, string)> TuDoanhHSX();
    }
    public class TuDoanhService : ITuDoanhService
    {
        private readonly ILogger<TuDoanhService> _logger;
        private readonly IAPIService _apiService;
        private readonly IFileService _fileService;
        private readonly IConfigDataRepo _configRepo;
        public TuDoanhService(ILogger<TuDoanhService> logger,
                                    IAPIService apiService,
                                    IFileService fileService,
                                    IConfigDataRepo configRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _fileService = fileService;
            _configRepo = configRepo;
        }

        public async Task<(int, string)> TuDoanhHSX()
        {
            var dt = DateTime.Now;
            var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}{dt.Day.To2Digit()}");
            try
            {
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, (int)EConfigDataType.TuDoanhHose);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);

                    _configRepo.DeleteMany(filter);
                }

                var strOutput = new StringBuilder();
                var stream = await _apiService.TuDoanhHSX(dt);
                if (stream is null
                    || stream.Length < 1000)
                    return (0, null);

                var lData = _fileService.HSX(stream);
                var lOutput = TuDoanhClean(lData);

                strOutput.AppendLine($"[Thông báo] Tự doanh HOSE ngày {dt.ToString("dd/MM/yyyy")}:");
                var lTopBuy = lOutput.Where(x => x.net > 0).OrderByDescending(x => x.net).Take(10);
                var lTopSell = lOutput.Where(x => x.net < 0).OrderBy(x => x.net).Take(10);
                if (lTopBuy.Any())
                {
                    strOutput.AppendLine($"*Top mua ròng:");
                }
                var index = 1;
                foreach (var item in lTopBuy)
                {
                    item.net = Math.Round(item.net / 1000000, 1);
                    item.net_pt = Math.Round(item.net_pt / 1000000, 1);
                    if (item.net == 0)
                        continue;

                    var content = $"{index++}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Mua ròng {Math.Abs(item.net).ToString("#,##0.#")} tỷ)";
                    if (item.net_pt != 0)
                    {
                        var buySell_pt = item.net_pt > 0 ? "Thỏa thuận mua" : "Thỏa thuận bán";
                        content += $" - {buySell_pt} {Math.Abs(item.net_pt).ToString("#,##0.#")} tỷ";
                    }
                    strOutput.AppendLine(content);
                }
                if (lTopSell.Any())
                {
                    strOutput.AppendLine();
                    strOutput.AppendLine($"*Top bán ròng:");
                }
                index = 1;
                foreach (var item in lTopSell)
                {
                    item.net = Math.Round(item.net / 1000000, 1);
                    item.net_pt = Math.Round(item.net_pt / 1000000, 1);
                    if (item.net == 0)
                        continue;

                    var content = $"{index++}. [{item.s}](https://finance.vietstock.vn/{item.s}/phan-tich-ky-thuat.htm) (Bán ròng {Math.Abs(item.net).ToString("#,##0.#")} tỷ)";
                    if (item.net_pt != 0)
                    {
                        var buySell_pt = item.net_pt > 0 ? "Thỏa thuận mua" : "Thỏa thuận bán";
                        content += $" - {buySell_pt} {Math.Abs(item.net_pt).ToString("#,##0.#")} tỷ";
                    }
                    strOutput.AppendLine(content);
                }

                _configRepo.InsertOne(new ConfigData
                {
                    ty = (int)EConfigDataType.TuDoanhHose,
                    t = t
                });

                return (1, strOutput.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"TuDoanhService.TuDoanhHSX|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private List<TuDoanh> TuDoanhClean(List<TudoanhPDF> lInput)
        {
            var lstResult = new List<TuDoanh>();
            foreach (var item in lInput)
            {
                if (string.IsNullOrWhiteSpace(item.s))
                    continue;

                var model = new TuDoanh
                {
                    no = item.no,
                    d = item.d,
                    s = item.s,
                    net_deal = item.bva - item.sva,
                    net_pt = item.bva_pt - item.sva_pt,
                    t = item.t
                };
                model.net = model.net_deal + model.net_pt;
                lstResult.Add(model);
            }
            return lstResult;
        }
    }
}
