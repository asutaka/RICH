using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        private async Task SyncBCTC(bool ghide = false)
        {
            try
            {
                var lStock = _stockRepo.GetAll();
                var lStockFilter = lStock.Where(x => x.status == 1 && !x.cat.Any(x => x.ty == (int)EStockType.NganHang
                                                                                    || x.ty == (int)EStockType.ChungKhoan
                                                                                    || x.ty == (int)EStockType.BDS)).Select(x => x.s);
                foreach (var item in lStockFilter)
                {
                    var builder = Builders<Financial>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.d, (int)StaticVal._currentTime.Item1),
                        builder.Eq(x => x.s, item)
                    );
                    var exists = _financialRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        if (ghide)
                        {
                            _financialRepo.DeleteMany(filter);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    await SyncBCTC_KQKD(item);
                    await SyncBCTC_CDKT(item, ECDKTType.Normal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_BatDongSan(bool ghide = false)
        {
            try
            {
                var lStockFilter = StaticVal._lStock.Where(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.BDS)).Select(x => x.s);
                foreach (var item in lStockFilter)
                {
                    var builder = Builders<Financial>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.d, (int)StaticVal._currentTime.Item1),
                        builder.Eq(x => x.s, item)
                    );
                    var exists = _financialRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        if (ghide)
                        {
                            _financialRepo.DeleteMany(filter);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    await SyncBCTC_KQKD(item);
                    await SyncBCTC_CDKT(item, ECDKTType.BDS);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_BatDongSan|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_KQKD(string code)
        {
            try
            {
                var time = GetCurrentTime();
                var lReportID = await _apiService.VietStock_KQKD_GetListReportData(code);
                Thread.Sleep(1000);
                var last = lReportID.data.LastOrDefault();
                if (last is null)
                    return;

                var year = last.BasePeriodBegin / 100;
                var month = last.BasePeriodBegin - year * 100;
                var quarter = 1;
                if (month >= 10)
                {
                    quarter = 4;
                }
                else if (month >= 7)
                {
                    quarter = 3;
                }
                else if (month >= 4)
                {
                    quarter = 2;
                }

                //check day
                var d = int.Parse($"{year}{quarter}");
                var fix = StaticVal._dicMa.FirstOrDefault(x => x.Key == code);
                if(fix.Key != null)
                {
                    d = int.Parse($"{year}{last.ReportTermID - 1}").AddQuarter(-fix.Value);
                }    

                if (d < (int)StaticVal._currentTime.Item1)
                    return;

                //insert
                var entity = new Financial
                {
                    d = d,
                    s = code,
                    t = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                };

                var strBuilder = new StringBuilder();
                strBuilder.Append($"StockCode={code}&");
                strBuilder.Append($"Unit=1000000000&");
                strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}&");
                strBuilder.Append($"listReportDataIds[0][ReportDataId]={last.ReportDataID}&");
                strBuilder.Append($"listReportDataIds[0][YearPeriod]={last.BasePeriodBegin / 100}");
                var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                var lData = await _apiService.VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(txt);
                Thread.Sleep(1000);
                if (!(lData?.data?.Any() ?? false))
                    return;

                var DoanhThu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.DoanhThu);
                var LoiNhuan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNST);
                var LoiNhuanGop = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNGop);
                var LoiNhuanRong = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNRong);
                AssignData(DoanhThu?.Value1, LoiNhuan?.Value1, LoiNhuanGop?.Value1, LoiNhuanRong?.Value1);
                _financialRepo.InsertOne(entity);

                void AssignData(double? DoanhThu, double? LoiNhuan, double? LoiNhuanGop, double? LoiNhuanRong)
                {
                    entity.rv = DoanhThu ?? 0;
                    entity.pf = LoiNhuan ?? 0;
                    entity.pfg = LoiNhuanGop ?? 0;
                    entity.pfn = LoiNhuanRong ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_CDKT(string code, ECDKTType type)//type: 1-Normal
        {
            try
            {
                var time = GetCurrentTime();
                var lReportID = await _apiService.VietStock_CDKT_GetListReportData(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                lReportID.data.Reverse();
                ReportDataIDDetailResponse last = null;
                var d = 0;
                foreach (var item in lReportID.data)
                {
                    var year = item.BasePeriodBegin / 100;
                    var month = item.BasePeriodBegin - year * 100;
                    var quarter = 1;
                    if (month >= 10)
                    {
                        quarter = 4;
                    }
                    else if (month >= 7)
                    {
                        quarter = 3;
                    }
                    else if (month >= 4)
                    {
                        quarter = 2;
                    }

                    //check day
                    d = int.Parse($"{year}{quarter}");
                    if (d != (int)StaticVal._currentTime.Item1)
                    {
                        d = 0;
                        continue;
                    }
                    last = item;
                    break;
                }

                if (d <= 0)
                    return;

                var strBuilder = new StringBuilder();
                strBuilder.Append($"StockCode={code}&");
                strBuilder.Append($"Unit=1000000000&");
                strBuilder.Append($"listReportDataIds[0][ReportDataId]={last.ReportDataID}&");
                strBuilder.Append($"listReportDataIds[0][YearPeriod]={last.BasePeriodBegin / 100}&");
                strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}");
                var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                var lData = await _apiService.VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(txt);
                Thread.Sleep(1000);
                if (!(lData?.data?.Any() ?? false))
                    return;

                var builder = Builders<Financial>.Filter;
                var filter = builder.And(
                     builder.Eq(x => x.s, code),
                    builder.Eq(x => x.d, (int)StaticVal._currentTime.Item1)
                );

                var entityUpdate = _financialRepo.GetEntityByFilter(filter);
                if (entityUpdate is null)
                {
                    return;
                }

                if (type == ECDKTType.BDS)
                {
                    entityUpdate = CDKT_BDS(entityUpdate, lData);
                }
                else if (type == ECDKTType.ChungKhoan)
                {
                    entityUpdate = CDKT_ChungKhoan(entityUpdate, lData);
                }
                else
                {
                    entityUpdate = CDKT_Normal(entityUpdate, lData);
                }
                _financialRepo.Update(entityUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_CDKT|EXCEPTION| {ex.Message}");
            }
        }

        private Financial CDKT_Normal(Financial entityUpdate, ReportDataDetailValue_BCTTResponse lData)
        {
            try
            {
                entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var TonKho = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TonKhoThep);
                var VayNganHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayNganHan);
                var VayDaiHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayDaiHan);
                var VonChuSH = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VonChuSoHuu);
                AssignData(TonKho?.Value1, VayNganHan?.Value1, VayDaiHan?.Value1, VonChuSH?.Value1);

                void AssignData(double? TonKho, double? VayNganHan, double? VayDaiHan, double? VonChuSH)
                {
                    entityUpdate.inv = TonKho ?? 0;
                    entityUpdate.debt = (VayNganHan ?? 0) + (VayDaiHan ?? 0);
                    entityUpdate.eq = VonChuSH ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.CDKT_Normal|EXCEPTION| {ex.Message}");
            }
            return entityUpdate;
        }

        private Financial CDKT_BDS(Financial entityUpdate, ReportDataDetailValue_BCTTResponse lData)
        {
            try
            {
                entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var TonKho = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TonKho);
                var NguoiMua = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.NguoiMuaTraTienTruoc);
                var VayNganHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayNganHan);
                var VayDaiHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayDaiHan);
                var VonChu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VonChuSoHuu);
                AssignData(TonKho?.Value1, NguoiMua?.Value1, VayNganHan?.Value1, VayDaiHan?.Value1, VonChu?.Value1);

                void AssignData(double? tonkho, double? nguoimua, double? vayNganHan, double? vayDaiHan, double? vonchu)
                {
                    entityUpdate.inv = tonkho ?? 0;
                    entityUpdate.bp = nguoimua ?? 0;
                    entityUpdate.debt = (vayNganHan ?? 0) + (vayDaiHan ?? 0);
                    entityUpdate.eq = vonchu ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.CDKT_Normal|EXCEPTION| {ex.Message}");
            }
            return entityUpdate;
        }

        private Financial CDKT_ChungKhoan(Financial entityUpdate, ReportDataDetailValue_BCTTResponse lData)
        {
            try
            {
                entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var TaiSanFVTPL = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanFVTPL);
                var TaiSanHTM = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanHTM);
                var TaiSanAFS = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanAFS);
                var TaiSanChoVay = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanChoVay);
                var VonChuSoHuuCK = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VonChuSoHuuCK);
                AssignData(TaiSanFVTPL?.Value1, TaiSanHTM?.Value1, TaiSanAFS?.Value1, TaiSanChoVay?.Value1, VonChuSoHuuCK?.Value1);

                void AssignData(double? TaiSanFVTPL, double? TaiSanHTM, double? TaiSanAFS, double? TaiSanChoVay, double? VonChuSoHuuCK)
                {
                    entityUpdate.itrade = TaiSanFVTPL ?? 0 + TaiSanHTM ?? 0 + TaiSanAFS ?? 0;
                    entityUpdate.debt = TaiSanChoVay ?? 0;
                    entityUpdate.eq = VonChuSoHuuCK ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.CDKT_Normal|EXCEPTION| {ex.Message}");
            }
            return entityUpdate;
        }
    }
}
