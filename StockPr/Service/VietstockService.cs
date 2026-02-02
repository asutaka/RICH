using Newtonsoft.Json;
using StockPr.Model;

namespace StockPr.Service
{
    public interface IVietstockService
    {
        Task<ReportDataIDResponse> VietStock_CDKT_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(string body);
        Task<ReportDataIDResponse> VietStock_KQKD_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(string body);
        Task<ReportTempIDResponse> VietStock_CSTC_GetListTempID(string code);
        Task<TempDetailValue_CSTCResponse> VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(string body);
        Task<ReportDataIDResponse> VietStock_TM_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_TM_ByReportDataIds(string body);
        Task<List<Vietstock_GICSProportionResponse>> VietStock_GetGICSProportion();
        Task<IEnumerable<BCTCAPIResponse>> VietStock_GetDanhSachBCTC(string code, int page);

        // Các phương thức trợ giúp dùng chung nội bộ trong VietstockService (nếu cần public)
        Task<ReportDataIDResponse> VietStock_GetListReportData(string code, string url);
        Task<ReportTempIDResponse> VietStock_GetListTempID(string code, string url);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue(string body, string url);
        Task<TempDetailValue_CSTCResponse> GetFinanceIndexDataValue(string body, string url);
    }
    public class VietstockService : IVietstockService
    {
        private readonly ILogger<VietstockService> _logger;
        private readonly IVietstockAuthService _authService;

        public VietstockService(ILogger<VietstockService> logger, IVietstockAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public async Task<ReportDataIDResponse> VietStock_CDKT_GetListReportData(string code)
        {
            var url = "https://finance.vietstock.vn/data/CDKT_GetListReportData";
            return await VietStock_GetListReportData(code, url);
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetReportDataDetailValueByReportDataIds";
            return await VietStock_GetReportDataDetailValue(body, url);
        }

        public async Task<ReportDataIDResponse> VietStock_KQKD_GetListReportData(string code)
        {
            var url = "https://finance.vietstock.vn/data/KQKD_GetListReportData";
            return await VietStock_GetListReportData(code, url);
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetReportDataDetailValue_KQKD_ByReportDataIds";
            return await VietStock_GetReportDataDetailValue(body, url);
        }

        public async Task<ReportTempIDResponse> VietStock_CSTC_GetListTempID(string code)
        {
            var url = "https://finance.vietstock.vn/data/CSTC_GetListTerms";
            return await VietStock_GetListTempID(code, url);
        }

        public async Task<TempDetailValue_CSTCResponse> VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetFinanceIndexDataValue_CSTC_ByListTerms";
            return await GetFinanceIndexDataValue(body, url);
        }

        public async Task<ReportDataIDResponse> VietStock_TM_GetListReportData(string code)
        {
            var url = "https://finance.vietstock.vn/data/TM_GetListReportData";
            return await VietStock_GetListReportData(code, url);
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_TM_ByReportDataIds(string body)
        {
            var url = "https://finance.vietstock.vn/data/GetReportDataDetailValue_TM_ByReportDataIds";
            return await VietStock_GetReportDataDetailValue(body, url);
        }

        public async Task<List<Vietstock_GICSProportionResponse>> VietStock_GetGICSProportion()
        {
            try
            {
                var url = "https://finance.vietstock.vn/Data/GetGICSProportion";
                var body = new Dictionary<string, string>
                {
                    ["level"] = "2",
                    ["duration"] = "D"
                };

                var responseStr = await _authService.PostAsync(url, body);
                if (string.IsNullOrEmpty(responseStr) || responseStr.Contains("<!DOCTYPE")) return null;

                var res = JsonConvert.DeserializeObject<List<Vietstock_GICSProportionResponse>>(responseStr);
                if (res != null)
                {
                    foreach (var item in res)
                    {
                        if (item.TradingDate.HasValue)
                        {
                            item.TradingDate = item.TradingDate.Value.AddHours(7);
                        }
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError($"VietstockService.VietStock_GetGICSProportion|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<IEnumerable<BCTCAPIResponse>> VietStock_GetDanhSachBCTC(string code, int page)
        {
            try
            {
                var url = "https://finance.vietstock.vn/data/getdocument";
                var body = new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["page"] = page.ToString(),
                    ["type"] = "1"
                };

                var responseStr = await _authService.PostAsync(url, body);
                if (string.IsNullOrEmpty(responseStr) || responseStr.Contains("<!DOCTYPE")) return null;

                return JsonConvert.DeserializeObject<IEnumerable<BCTCAPIResponse>>(responseStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"VietstockService.VietStock_GetDanhSachBCTC|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<ReportDataIDResponse> VietStock_GetListReportData(string code, string url)
        {
            try
            {
                var body = new Dictionary<string, string>
                {
                    ["StockCode"] = code,
                    ["UnitedId"] = "-1",
                    ["AuditedStatusId"] = "-1",
                    ["Unit"] = "1000000000",
                    ["IsNamDuongLich"] = "false",
                    ["PeriodType"] = "QUY",
                    ["SortTimeType"] = "Time_ASC"
                };

                var responseStr = await _authService.PostAsync(url, body);
                if (string.IsNullOrEmpty(responseStr)) return null;

                return JsonConvert.DeserializeObject<ReportDataIDResponse>(responseStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"VietstockService.VietStock_GetListReportData|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<ReportTempIDResponse> VietStock_GetListTempID(string code, string url)
        {
            try
            {
                var body = new Dictionary<string, string>
                {
                    ["StockCode"] = code,
                    ["UnitedId"] = "-1",
                    ["AuditedStatusId"] = "-1",
                    ["Unit"] = "1000000000",
                    ["IsNamDuongLich"] = "false",
                    ["PeriodType"] = "QUY",
                    ["SortTimeType"] = "Time_ASC"
                };

                var responseStr = await _authService.PostAsync(url, body);
                if (string.IsNullOrEmpty(responseStr)) return null;

                return JsonConvert.DeserializeObject<ReportTempIDResponse>(responseStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"VietstockService.VietStock_GetListTempID|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue(string body, string url)
        {
            try
            {
                var responseStr = await _authService.PostAsync(url, body);
                if (string.IsNullOrEmpty(responseStr)) return null;

                return JsonConvert.DeserializeObject<ReportDataDetailValue_BCTTResponse>(responseStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"VietstockService.VietStock_GetReportDataDetailValue|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task<TempDetailValue_CSTCResponse> GetFinanceIndexDataValue(string body, string url)
        {
            try
            {
                var responseStr = await _authService.PostAsync(url, body);
                if (string.IsNullOrEmpty(responseStr)) return null;

                return JsonConvert.DeserializeObject<TempDetailValue_CSTCResponse>(responseStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"VietstockService.GetFinanceIndexDataValue|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
