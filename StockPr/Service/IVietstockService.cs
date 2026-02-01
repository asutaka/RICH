using StockPr.Model;
using StockPr.Model.BCPT;

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
}
