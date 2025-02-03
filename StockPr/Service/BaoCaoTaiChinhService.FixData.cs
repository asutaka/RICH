using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        public async Task BCTC_FixData(string code)
        {
            try
            {
                var isChungKhoan = StaticVal._lStock.Any(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.ChungKhoan) && x.s == code);
                if (isChungKhoan)
                {
                    await BCTC_FixData_ChungKhoan(code);
                    return;
                }

                var isBDS = StaticVal._lStock.Any(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.BDS) && x.s == code);
                if (isChungKhoan)
                {
                    await BCTC_FixData_BDS(code);
                    return;
                }

                await BCTC_FixData_CK(code);
            }
            catch(Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.FixData|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_ChungKhoan(string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_ChungKhoan|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_BDS(string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_BDS|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_CK(string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_CK|EXCEPTION| {ex.Message}");
            }
        }
    }
}
