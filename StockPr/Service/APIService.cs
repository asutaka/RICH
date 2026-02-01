using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockPr.Model;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public interface IAPIService
    {
        Task<Stream> GetChartImage(string body);

        Task<(bool, List<DSC_Data>)> DSC_GetPost();
        Task<(bool, List<VNDirect_Data>)> VNDirect_GetPost(bool isIndustry);
        Task<(bool, List<MigrateAsset_Data>)> MigrateAsset_GetPost();
        Task<(bool, List<AGR_Data>)> Agribank_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> SSI_GetPost(bool isIndustry);
        Task<(bool, List<VCI_Content>)> VCI_GetPost();
        Task<(bool, List<VCBS_Data>)> VCBS_GetPost();
        Task<(bool, List<BCPT_Crawl_Data>)> BSC_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> MBS_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> PSI_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> FPTS_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> KBS_GetPost(bool isIndustry);
        Task<(bool, List<BCPT_Crawl_Data>)> CafeF_GetPost();

        Task<List<Pig333_Clean>> Pig333_GetPigPrice();
        Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities();
        Task<MacroMicro_Key> MacroMicro_WCI(string key);
        Task<List<Metal_Detail>> Metal_GetYellowPhotpho();

        Task<string> TongCucThongKeGetUrl();
        Task<Stream> TongCucThongKeGetFile(string url);

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

        Task<ReportDataIDResponse> VietStock_CDKT_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(string body);
        Task<ReportDataIDResponse> VietStock_KQKD_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(string body);
        Task<ReportTempIDResponse> VietStock_CSTC_GetListTempID(string code);
        Task<TempDetailValue_CSTCResponse> VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(string body);
        Task<ReportDataIDResponse> VietStock_TM_GetListReportData(string code);
        Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_TM_ByReportDataIds(string body);
        Task<IEnumerable<BCTCAPIResponse>> VietStock_GetDanhSachBCTC(string code, int page);
        Task<List<Vietstock_GICSProportionResponse>> VietStock_GetGICSProportion();

        Task<List<F319Model>> F319_Scout(string acc);

        Task<List<string>> News_NguoiQuanSat();
        Task<News_KinhTeChungKhoan> News_KinhTeChungKhoan();
        Task<List<News_Raw>> News_NguoiDuaTin();
        Task SBV_OMO();
    }
    public class APIService : IAPIService
    {
        private readonly ILogger<APIService> _logger;
        private readonly IHttpClientFactory _client;
        private readonly IScraperService _scraperService;
        private readonly IMarketDataService _marketDataService;
        private readonly IVietstockService _vietstockService;
        private readonly IMacroDataService _macroDataService;
        private readonly IHighChartService _highChartService;
        public APIService(ILogger<APIService> logger,
                        IHttpClientFactory httpClientFactory,
                        IScraperService scraperService,
                        IMarketDataService marketDataService,
                        IVietstockService vietstockService,
                        IMacroDataService macroDataService,
                        IHighChartService highChartService)
        {
            _logger = logger;
            _client = httpClientFactory;
            _scraperService = scraperService;
            _marketDataService = marketDataService;
            _vietstockService = vietstockService;
            _macroDataService = macroDataService;
            _highChartService = highChartService;
        }
        public Task<Stream> GetChartImage(string body) => _highChartService.GetChartImage(body);

        #region Báo cáo phân tích
        public Task<(bool, List<DSC_Data>)> DSC_GetPost() => _scraperService.DSC_GetPost();
        public Task<(bool, List<VNDirect_Data>)> VNDirect_GetPost(bool isIndustry) => _scraperService.VNDirect_GetPost(isIndustry);
        public Task<(bool, List<MigrateAsset_Data>)> MigrateAsset_GetPost() => _scraperService.MigrateAsset_GetPost();
        public Task<(bool, List<AGR_Data>)> Agribank_GetPost(bool isIndustry) => _scraperService.Agribank_GetPost(isIndustry);
        public Task<(bool, List<BCPT_Crawl_Data>)> SSI_GetPost(bool isIndustry) => _scraperService.SSI_GetPost(isIndustry);
        public Task<(bool, List<VCI_Content>)> VCI_GetPost() => _scraperService.VCI_GetPost();
        public Task<(bool, List<VCBS_Data>)> VCBS_GetPost() => _scraperService.VCBS_GetPost();
        public Task<(bool, List<BCPT_Crawl_Data>)> BSC_GetPost(bool isIndustry) => _scraperService.BSC_GetPost(isIndustry);
        public Task<(bool, List<BCPT_Crawl_Data>)> MBS_GetPost(bool isIndustry) => _scraperService.MBS_GetPost(isIndustry);
        public Task<(bool, List<BCPT_Crawl_Data>)> PSI_GetPost(bool isIndustry) => _scraperService.PSI_GetPost(isIndustry);
        public Task<(bool, List<BCPT_Crawl_Data>)> FPTS_GetPost(bool isIndustry) => _scraperService.FPTS_GetPost(isIndustry);
        public Task<(bool, List<BCPT_Crawl_Data>)> KBS_GetPost(bool isIndustry) => _scraperService.KBS_GetPost(isIndustry);
        public Task<(bool, List<BCPT_Crawl_Data>)> CafeF_GetPost() => _scraperService.CafeF_GetPost();
        #endregion

        #region Giá Ngành Hàng
        public Task<List<Pig333_Clean>> Pig333_GetPigPrice() => _macroDataService.Pig333_GetPigPrice();
        public Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities() => _macroDataService.Tradingeconimic_Commodities();
        public Task<MacroMicro_Key> MacroMicro_WCI(string key) => _macroDataService.MacroMicro_WCI(key);
        public Task<List<Metal_Detail>> Metal_GetYellowPhotpho() => _macroDataService.Metal_GetYellowPhotpho();
        #endregion

        public Task<string> TongCucThongKeGetUrl() => _macroDataService.TongCucThongKeGetUrl();
        public Task<Stream> TongCucThongKeGetFile(string url) => _macroDataService.TongCucThongKeGetFile(url);

        public Task<Stream> TuDoanhHSX(DateTime dt) => _marketDataService.TuDoanhHSX(dt);
        public Task<List<Money24h_PTKTResponse>> Money24h_GetMaTheoChiBao(string chibao) => _marketDataService.Money24h_GetMaTheoChiBao(chibao);
        public Task<List<Money24h_ForeignResponse>> Money24h_GetForeign(EExchange mode, EMoney24hTimeType type) => _marketDataService.Money24h_GetForeign(mode, type);
        public Task<List<Money24h_TuDoanhResponse>> Money24h_GetTuDoanh(EExchange mode, EMoney24hTimeType type) => _marketDataService.Money24h_GetTuDoanh(mode, type);
        public Task<Money24h_NhomNganhResponse> Money24h_GetNhomNganh(EMoney24hTimeType type) => _marketDataService.Money24h_GetNhomNganh(type);
        public Task<Money24h_StatisticResponse> Money24h_GetThongke(string sym = "10") => _marketDataService.Money24h_GetThongke(sym);

        public Task<List<Quote>> Vietstock_GetDataStock(string code) => _marketDataService.Vietstock_GetDataStock(code);
        public Task<List<Quote>> SSI_GetDataStock(string code) => _marketDataService.SSI_GetDataStock(code);
        public Task<List<QuoteT>> SSI_GetDataStockT(string code) => _marketDataService.SSI_GetDataStockT(code);
        public Task<List<Quote>> SSI_GetDataStock_HOUR(string code) => _marketDataService.SSI_GetDataStock_HOUR(code);
        public Task<SSI_DataFinanceDetailResponse> SSI_GetFinanceStock(string code) => _marketDataService.SSI_GetFinanceStock(code);
        public Task<decimal> SSI_GetFreefloatStock(string code) => _marketDataService.SSI_GetFreefloatStock(code);
        public Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code) => _marketDataService.SSI_GetStockInfo(code);
        public Task<SSI_DataStockInfoResponse> SSI_GetStockInfo(string code, DateTime from, DateTime to) => _marketDataService.SSI_GetStockInfo(code, from, to);
        public Task<SSI_DataStockInfoResponse> SSI_GetStockInfo_Extend(string code, DateTime from, DateTime to) => _marketDataService.SSI_GetStockInfo_Extend(code, from, to);
        public Task<VNDirect_ForeignDetailResponse> VNDirect_GetForeign(string code) => _marketDataService.VNDirect_GetForeign(code);

        public Task<List<F319Model>> F319_Scout(string acc) => _scraperService.F319_Scout(acc);
        public Task SBV_OMO() => _marketDataService.SBV_OMO();

        #region Báo cáo tài chính
        public Task<ReportDataIDResponse> VietStock_CDKT_GetListReportData(string code) => _vietstockService.VietStock_CDKT_GetListReportData(code);
        public Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(string body) => _vietstockService.VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(body);
        public Task<ReportDataIDResponse> VietStock_KQKD_GetListReportData(string code) => _vietstockService.VietStock_KQKD_GetListReportData(code);
        public Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(string body) => _vietstockService.VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(body);
        public Task<ReportTempIDResponse> VietStock_CSTC_GetListTempID(string code) => _vietstockService.VietStock_CSTC_GetListTempID(code);
        public Task<TempDetailValue_CSTCResponse> VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(string body) => _vietstockService.VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(body);
        public Task<ReportDataIDResponse> VietStock_TM_GetListReportData(string code) => _vietstockService.VietStock_TM_GetListReportData(code);
        public Task<ReportDataDetailValue_BCTTResponse> VietStock_GetReportDataDetailValue_TM_ByReportDataIds(string body) => _vietstockService.VietStock_GetReportDataDetailValue_TM_ByReportDataIds(body);
        public Task<List<Vietstock_GICSProportionResponse>> VietStock_GetGICSProportion() => _vietstockService.VietStock_GetGICSProportion();
        public Task<IEnumerable<BCTCAPIResponse>> VietStock_GetDanhSachBCTC(string code, int page) => _vietstockService.VietStock_GetDanhSachBCTC(code, page);
        #endregion

        #region NEWS
        public Task<List<string>> News_NguoiQuanSat() => _scraperService.News_NguoiQuanSat();
        public Task<News_KinhTeChungKhoan> News_KinhTeChungKhoan() => _scraperService.News_KinhTeChungKhoan();
        public Task<List<News_Raw>> News_NguoiDuaTin() => _scraperService.News_NguoiDuaTin();
        #endregion
    }
}
