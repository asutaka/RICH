using MongoDB.Driver;
using Newtonsoft.Json;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace StockPr.Service
{
    public interface IChartService
    {
        Task<List<InputFileStream>> Chart_MaCK(string input);
    }
    public class ChartService : IChartService
    {
        private readonly ILogger<ChartService> _logger;
        private readonly IFinancialRepo _financialRepo;
        private readonly ICommonService _commonService;
        private readonly IAPIService _apiService;
        public ChartService(ILogger<ChartService> logger, ICommonService commonService, IAPIService apiService, IFinancialRepo financialRepo) 
        {
            _logger = logger;
            _commonService = commonService;
            _apiService = apiService;
            _financialRepo = financialRepo;
        }

        public async Task<List<InputFileStream>> Chart_MaCK(string input)
        {
            var lRes = new List<InputFileStream>();
            try
            {
                var StockPr = StaticVal._lStock.FirstOrDefault(x => x.s == input);
                if (StockPr == null)
                    return null;

                var lFinancial = _financialRepo.GetByFilter(Builders<Financial>.Filter.Eq(x => x.s, input));
                if (!lFinancial.Any())
                    return null;

                var lStream = new List<Stream>();
                var streamDoanhThu = await Chart_DoanhThu_LoiNhuan(lFinancial.Select(x => new BaseFinancialDTO { d = x.d, rv = x.rv, pf = x.pf }).ToList(), input);
                lStream.Add(streamDoanhThu);

                lRes = lStream.Select(x => InputFile.FromStream(x)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ChartService.Chart_MaCK|EXCEPTION| {ex.Message}");
            }
            return lRes;
        }

        public async Task<Stream> Chart_DoanhThu_LoiNhuan(List<BaseFinancialDTO> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();
                var lTangTruong = new List<double>();
                foreach (var item in lFinancial)
                {
                    double tangTruong = 0;
                    var prevQuarter = item.d.GetYoyQuarter();
                    var prev = lFinancial.FirstOrDefault(x => x.d == prevQuarter);
                    if (prev is not null && prev.pf != 0)
                    {
                        tangTruong = Math.Round(100 * (-1 + item.pf / prev.pf), 1);
                        if (item.pf > prev.pf)
                        {
                            tangTruong = Math.Abs(tangTruong);
                        }
                        if (tangTruong >= StaticVal._MaxRate)
                        {
                            tangTruong = StaticVal._MaxRate;
                        }
                        if (tangTruong <= -StaticVal._MaxRate)
                        {
                            tangTruong = -StaticVal._MaxRate;
                        }
                    }

                    lTangTruong.Add(tangTruong);
                }
                var lTake = lFinancial.TakeLast(StaticVal._TAKE);

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lTake.Select(x => x.rv),
                        name = "Doanh thu",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                     new HighChartSeries_BasicColumn
                    {
                        data = lTake.Select(x => x.pf),
                        name = "Lợi nhuận",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lTangTruong.TakeLast(StaticVal._TAKE),
                        name = "Tăng trưởng lợi nhuận",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#C00000",
                        yAxis = 1,
                    }
                };

                return await Chart_BasicBase($"{code} - Doanh thu, Lợi nhuận Quý {time.Item3}/{time.Item2} (QoQ)", lTake.Select(x => x.d.GetNameQuarter()).ToList(), lSeries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_BDS_DoanhThu_LoiNhuan|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<Stream> Chart_BasicBase(string title, List<string> lCat, List<HighChartSeries_BasicColumn> lSerie, string titleX = null, string titleY = null)
        {
            try
            {
                var basicColumn = new HighchartBasicColumn(title, lCat, lSerie);
                var strX = string.IsNullOrWhiteSpace(titleX) ? "(Đơn vị: tỷ)" : titleX;
                var strY = string.IsNullOrWhiteSpace(titleY) ? "(Tỉ lệ: %)" : titleY;

                basicColumn.yAxis = new List<HighChartYAxis> { new HighChartYAxis { title = new HighChartTitle { text = strX }, labels = new HighChartLabel{ format = "{value}" } },
                                                                 new HighChartYAxis { title = new HighChartTitle { text = strY }, labels = new HighChartLabel{ format = "{value}" }, opposite = true }};

                var chart = new HighChartModel(JsonConvert.SerializeObject(basicColumn));
                var body = JsonConvert.SerializeObject(chart);
                return await _apiService.GetChartImage(body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_BasicBase|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
