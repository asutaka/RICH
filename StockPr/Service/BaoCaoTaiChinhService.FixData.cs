namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        public async Task BCTC_FixData(string code)
        {
            try
            {

            }
            catch(Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.FixData|EXCEPTION| {ex.Message}");
            }
        }

        public async Task BCTC_FixData_DoanhThuLoiNhuan()
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.FixDataDoanhThuLoiNhuan|EXCEPTION| {ex.Message}");
            }
        }
    }
}
