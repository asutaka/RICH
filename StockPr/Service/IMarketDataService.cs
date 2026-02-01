using Skender.Stock.Indicators;
using StockPr.Model;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface IMarketDataService
    {
        Task<Stream> TuDoanhHSX(DateTime dt);
        Task<List<Quote>> Vietstock_GetDataStock(string code);
        Task<List<Quote>> SSI_GetDataStock(string code);
        Task<List<QuoteT>> SSI_GetDataStockT(string code);
        Task<List<Quote>> SSI_GetDataStock_HOUR(string code);
        Task<SSI_DataFinanceDetailResponse> SSI_GetFinanceStock(string code);
        Task<decimal> SSI_GetFreefloatStock(string code);
        Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code);
        Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code, DateTime from, DateTime to);
        Task<SSI_DataStockInfoResponse> SSI_GetStockInfo_Extend(string code, DateTime from, DateTime to);
        Task<VNDirect_ForeignDetailResponse> VNDirect_GetForeign(string code);
        Task<List<Money24h_PTKTResponse>> Money24h_GetMaTheoChiBao(string chibao);
        Task<List<Money24h_ForeignResponse>> Money24h_GetForeign(EExchange mode, EMoney24hTimeType type);
        Task<List<Money24h_TuDoanhResponse>> Money24h_GetTuDoanh(EExchange mode, EMoney24hTimeType type);
        Task<Money24h_NhomNganhResponse> Money24h_GetNhomNganh(EMoney24hTimeType type);
        Task<Money24h_StatisticResponse> Money24h_GetThongke(string sym = "10");
        Task SBV_OMO();
    }
}
