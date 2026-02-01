using StockPr.Model;
using StockPr.Model.BCPT;

namespace StockPr.Service
{
    public interface IScraperService
    {
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
        Task<List<string>> News_NguoiQuanSat();
        Task<News_KinhTeChungKhoan> News_KinhTeChungKhoan();
        Task<List<News_Raw>> News_NguoiDuaTin();
        Task<List<F319Model>> F319_Scout(string acc);
    }
}
