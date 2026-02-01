using StockPr.Model;
using StockPr.Model.BCPT;

namespace StockPr.Parser
{
    public interface IScraperParser
    {
        List<DSC_Data> ParseDSC(string html);
        List<VNDirect_Data> ParseVNDirect(string html);
        List<MigrateAsset_Data> ParseMigrateAsset(string html);
        List<AGR_Data> ParseAgribank(string html);
        List<BCPT_Crawl_Data> ParseSSI(string html);
        List<VCI_Content> ParseVCI(string html);
        List<VCBS_Data> ParseVCBS(string html);
        List<BCPT_Crawl_Data> ParseBSC(string html);
        List<BCPT_Crawl_Data> ParseMBS(string html);
        List<BCPT_Crawl_Data> ParsePSI(string html);
        List<BCPT_Crawl_Data> ParseFPTS(string html);
        List<BCPT_Crawl_Data> ParseKBS(string html);
        List<BCPT_Crawl_Data> ParseCafeF(string html);
        List<string> ParseNguoiQuanSat(string html);
        News_KinhTeChungKhoan ParseKinhTeChungKhoan(string html);
        List<News_Raw> ParseNguoiDuaTin(string html);
        List<F319Model> ParseF319(string templateHtml);
    }
}
