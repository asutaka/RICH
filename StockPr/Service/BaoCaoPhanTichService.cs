using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;
using System.Text;
using System.Web;

namespace StockPr.Service
{
    public interface IBaoCaoPhanTichService
    {
        Task<string> BaoCaoPhanTich();
    }
    public class BaoCaoPhanTichService : IBaoCaoPhanTichService
    {
        private readonly ILogger<BaoCaoPhanTichService> _logger;
        private readonly IConfigBaoCaoPhanTichRepo _bcptRepo;
        private readonly IAPIService _apiService;
        public BaoCaoPhanTichService(ILogger<BaoCaoPhanTichService> logger,
                                IAPIService apiService,
                                IConfigBaoCaoPhanTichRepo bcptRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _bcptRepo = bcptRepo;
        }

        public async Task<string> BaoCaoPhanTich()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var ssi_COM = await SSI(false);
                if (!string.IsNullOrWhiteSpace(ssi_COM))
                {
                    sBuilder.Append(ssi_COM);
                }

                var vcbs = await VCBS();
                if (!string.IsNullOrWhiteSpace(vcbs))
                {
                    sBuilder.Append(vcbs);
                }

                var vci = await VCI();
                if (!string.IsNullOrWhiteSpace(vci))
                {
                    sBuilder.Append(vci);
                }

                var dsc = await DSC();
                if(!string.IsNullOrWhiteSpace(dsc))
                {
                    sBuilder.Append(dsc);
                }

                var vndirect_COM = await VNDirect(false);
                if (!string.IsNullOrWhiteSpace(vndirect_COM))
                {
                    sBuilder.Append(vndirect_COM);
                }

                var mbs_COM = await MBS(false);
                if (!string.IsNullOrWhiteSpace(mbs_COM))
                {
                    sBuilder.Append(mbs_COM);
                }

                var fpts_COM = await FPTS(false);
                if (!string.IsNullOrWhiteSpace(fpts_COM))
                {
                    sBuilder.Append(fpts_COM);
                }

                var bsc_COM = await BSC(false);
                if (!string.IsNullOrWhiteSpace(bsc_COM))
                {
                    sBuilder.Append(bsc_COM);
                }

                var ma = await MigrateAsset();
                if (!string.IsNullOrWhiteSpace(ma))
                {
                    sBuilder.Append(ma);
                }

                var agribank_COM = await Agribank(false);
                if (!string.IsNullOrWhiteSpace(agribank_COM))
                {
                    sBuilder.Append(agribank_COM);
                }

                var psi_COM = await PSI(false);
                if (!string.IsNullOrWhiteSpace(psi_COM))
                {
                    sBuilder.Append(psi_COM);
                }

                var kbs_COM = await KBS(false);
                if (!string.IsNullOrWhiteSpace(kbs_COM))
                {
                    sBuilder.Append(kbs_COM);
                }


                var ssi_Ins = await SSI(true);
                if (!string.IsNullOrWhiteSpace(ssi_Ins))
                {
                    sBuilder.Append(ssi_Ins);
                }

                var vndirect_Ins = await VNDirect(true);
                if (!string.IsNullOrWhiteSpace(vndirect_Ins))
                {
                    sBuilder.Append(vndirect_Ins);
                }

                var mbs_Ins = await MBS(true);
                if (!string.IsNullOrWhiteSpace(mbs_Ins))
                {
                    sBuilder.Append(mbs_Ins);
                }

                var fpts_Ins = await FPTS(true);
                if (!string.IsNullOrWhiteSpace(fpts_Ins))
                {
                    sBuilder.Append(fpts_Ins);
                }

                var bsc_Ins = await BSC(true);
                if (!string.IsNullOrWhiteSpace(bsc_Ins))
                {
                    sBuilder.Append(bsc_Ins);
                }

                var agribank_Ins = await Agribank(true);
                if (!string.IsNullOrWhiteSpace(agribank_Ins))
                {
                    sBuilder.Append(agribank_Ins);
                }

                var psi_Ins = await PSI(true);
                if (!string.IsNullOrWhiteSpace(psi_Ins))
                {
                    sBuilder.Append(psi_Ins);
                }

                var kbs_Ins = await KBS(true);
                if (!string.IsNullOrWhiteSpace(kbs_Ins))
                {
                    sBuilder.Append(kbs_Ins);
                }

                var cafef = await CafeF();
                if (!string.IsNullOrWhiteSpace(cafef))
                {
                    sBuilder.Append(cafef);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.BaoCaoPhanTich|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> DSC()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");

                var lRes = await _apiService.DSC_GetPost();
                if (lRes is null)
                {
                    return string.Empty;
                }

                var lValid = lRes.Where(x => x.attributes.public_at > time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.DSC),
                                builder.Eq(x => x.key, itemValid.id.ToString()),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id.ToString(),
                            ty = (int)ESource.DSC
                        });

                        if (itemValid.attributes.category_id.data.attributes.slug.Equals("phan-tich-doanh-nghiep"))
                        {
                            var code = itemValid.attributes.slug.Split('-').First().ToUpper();
                            sBuilder.AppendLine($"[DSC - Phân tích cổ phiếu] - {code}:{itemValid.attributes.title}");
                            sBuilder.AppendLine($"Link: www.dsc.com.vn/bao-cao-phan-tich/{itemValid.attributes.slug}");
                        }
                        else if (!itemValid.attributes.category_id.data.attributes.slug.Contains("beat"))
                        {
                            sBuilder.AppendLine($"[DSC - Báo cáo phân tích]");
                            sBuilder.AppendLine($"Link: www.dsc.com.vn/bao-cao-phan-tich/{itemValid.attributes.slug}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.DSC|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> VNDirect(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var lRes = await _apiService.VNDirect_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[VNDirect - Báo cáo ngành]" : "[VNDirect - Phân tích cổ phiếu]";
                var commonlink = mode ? "https://dstock.vndirect.com.vn/trung-tam-phan-tich/bao-cao-nganh" 
                                    : "https://dstock.vndirect.com.vn/trung-tam-phan-tich/bao-cao-phan-tich-dn";

                var t = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lValid = lRes.Where(x => int.Parse(x.newsDate.Replace("-", "")) >= t);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.VNDirect),
                                builder.Eq(x => x.key, itemValid.newsId),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.newsId,
                            ty = (int)ESource.VNDirect
                        });

                        sBuilder.AppendLine($"{title} {itemValid.newsTitle}");
                        if (itemValid.attachments.Any())
                        {
                            var link = $"https://www.vndirect.com.vn/cmsupload/beta/{itemValid.attachments.First().name}";
                            sBuilder.AppendLine(link);
                        }
                        else
                        {
                            sBuilder.AppendLine($"Link: {commonlink}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VNDirect|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> MigrateAsset()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var lRes = await _apiService.MigrateAsset_GetPost();
                if (lRes is null)
                    return string.Empty;

                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");

                var lValid = lRes.Where(x => x.published_at > time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.MigrateAsset),
                                builder.Eq(x => x.key, itemValid.id.ToString()),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id.ToString(),
                            ty = (int)ESource.MigrateAsset
                        });


                        if (itemValid.stock_related.Length == 3)
                        {
                            sBuilder.AppendLine($"[MigrateAsset - Phân tích cổ phiếu] {itemValid.stock_related}:{itemValid.title}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"[MigrateAsset - Báo cáo phân tích] {itemValid.title}");
                        }
                        sBuilder.AppendLine($"Link: https://masvn.com/api{itemValid.file_path}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.MigrateAsset|EXCEPTION| {ex.Message}");
            }
            return sBuilder.ToString();
        }

        private async Task<string> Agribank(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[Agribank - Báo cáo ngành]" : "[Agribank - Phân tích cổ phiếu]";

                var lRes = await _apiService.Agribank_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.Date > time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.Agribank),
                                builder.Eq(x => x.key, itemValid.ReportID.ToString()),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.ReportID.ToString(),
                            ty = (int)ESource.Agribank
                        });

                        if (itemValid.Title.Contains("AGR Snapshot"))
                        {
                            sBuilder.AppendLine($"{title} {itemValid.Title.Replace("AGR Snapshot", "").Trim()}");
                            sBuilder.AppendLine($"Link: https://agriseco.com.vn/Report/ReportFile/{itemValid.ReportID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.Agribank|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> SSI(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[SSI - Báo cáo ngành]" : "[SSI - Phân tích cổ phiếu]";
                var commonlink = mode ? "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-nganh"
                                    : "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-cong-ty";

                var lRes = await _apiService.SSI_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.SSI),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.SSI
                        });

                        sBuilder.AppendLine($"{title} {itemValid.title}");
                        sBuilder.AppendLine($"Link: {commonlink}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.SSI|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> VCI()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lRes = await _apiService.VCI_GetPost();
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.makerDate >= time
                                            && (x.pageLink == "company-research" || x.pageLink == "sector-reports" || x.pageLink == "macroeconomics" || x.pageLink == "phan-tich-doanh-nghiep"));
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.VCI),
                                builder.Eq(x => x.key, itemValid.id.ToString()),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id.ToString(),
                            ty = (int)ESource.VCI
                        });

                        if (itemValid.pageLink == "company-research")
                        {
                            sBuilder.AppendLine($"[VCI - Phân tích cổ phiếu] {itemValid.name}");
                        }
                        else if (itemValid.pageLink == "sector-reports")
                        {
                            sBuilder.AppendLine($"[VCI - Báo cáo Ngành] {itemValid.name}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"[VCI - Báo cáo vĩ mô] {itemValid.name}");
                        }

                        sBuilder.AppendLine($"Link: {itemValid.file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VCI|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> VCBS()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lRes = await _apiService.VCBS_GetPost();
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.publishedAt >= time
                                            && (x.category.code == "BCVM" || x.category.code == "BCDN" || x.category.code == "BCN"));
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.VCBS),
                                builder.Eq(x => x.key, itemValid.id.ToString()),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id.ToString(),
                            ty = (int)ESource.VCBS
                        });

                        if (itemValid.category.code == "BCDN")
                        {
                            sBuilder.AppendLine($"[VCBS - Phân tích cổ phiếu] {itemValid.name}");
                        }
                        else if (itemValid.category.code == "BCN")
                        {
                            sBuilder.AppendLine($"[VCBS - Báo cáo Ngành] {itemValid.name}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"[VCBS - Báo cáo vĩ mô] {itemValid.name}");
                        }

                        sBuilder.AppendLine($"Link: https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VCBS|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> BSC(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[BSC - Báo cáo ngành]" : "[BSC - Phân tích cổ phiếu]";
                var commonlink = mode ? "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/2"
                                    : "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/1";

                var lRes = await _apiService.BSC_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.BSC),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.BSC
                        });

                        sBuilder.AppendLine($"{title} {itemValid.title}");
                        if (string.IsNullOrWhiteSpace(itemValid.path))
                        {
                            sBuilder.AppendLine($"Link: {commonlink}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"Link: {itemValid.path}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.BSC|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> MBS(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[MBS - Báo cáo ngành]" : "[MBS - Phân tích cổ phiếu]";
                var commonlink = mode ? "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/bao-cao-phan-tich-nganh/"
                                    : "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/nghien-cuu-co-phieu/";

                var lRes = await _apiService.MBS_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.MBS),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.MBS
                        });

                        sBuilder.AppendLine($"{title} {HttpUtility.HtmlDecode(itemValid.title)}");
                        if (string.IsNullOrWhiteSpace(itemValid.path))
                        {
                            sBuilder.AppendLine($"Link: {commonlink}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"Link: {HttpUtility.HtmlDecode(itemValid.path)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.MBS|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> PSI(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[PSI - Báo cáo ngành]" : "[PSI - Phân tích cổ phiếu]";
                var commonlink = mode ? "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-nganh"
                                    : "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-phan-tich-doanh-nghiep";

                var lRes = await _apiService.PSI_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.PSI),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.PSI
                        });

                        sBuilder.AppendLine($"{title} {itemValid.title}");
                        if (string.IsNullOrWhiteSpace(itemValid.path))
                        {
                            sBuilder.AppendLine($"Link: {commonlink}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"Link: {itemValid.path}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.PSI|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> FPTS(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[FPTS - Báo cáo ngành]" : "[FPTS - Phân tích cổ phiếu]";
                var commonlink = mode ? "https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=174"
                                    : "https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=179";

                var lRes = await _apiService.FPTS_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.FPTS),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.FPTS
                        });

                        sBuilder.AppendLine($"{title} {itemValid.title}");
                        if (string.IsNullOrWhiteSpace(itemValid.path))
                        {
                            sBuilder.AppendLine($"Link: {commonlink}");
                        }
                        else
                        {
                            sBuilder.AppendLine($"Link: {itemValid.path}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.FPTS|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> KBS(bool mode)
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[KBS - Báo cáo ngành]" : "[KBS - Phân tích cổ phiếu]";

                var lRes = await _apiService.KBS_GetPost(mode);
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.KBS),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.KBS
                        });

                        sBuilder.AppendLine($"{title} {itemValid.title}");
                        sBuilder.AppendLine($"Link: {itemValid.path}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.KBS|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<string> CafeF()
        {
            var sBuilder = new StringBuilder();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lRes = await _apiService.CafeF_GetPost();
                if (lRes is null)
                    return string.Empty;

                var lValid = lRes.Where(x => x.date >= time);
                if (lValid?.Any() ?? false)
                {
                    foreach (var itemValid in lValid)
                    {
                        FilterDefinition<ConfigBaoCaoPhanTich> filter = null;
                        var builder = Builders<ConfigBaoCaoPhanTich>.Filter;
                        var lFilter = new List<FilterDefinition<ConfigBaoCaoPhanTich>>()
                            {
                                builder.Eq(x => x.d, d),
                                builder.Eq(x => x.ty, (int)ESource.CafeF),
                                builder.Eq(x => x.key, itemValid.id),
                            };
                        foreach (var item in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = item;
                                continue;
                            }
                            filter &= item;
                        }
                        var entityValid = _bcptRepo.GetEntityByFilter(filter);
                        if (entityValid != null)
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id,
                            ty = (int)ESource.CafeF
                        });

                        sBuilder.AppendLine($"[CafeF - Phân tích] {itemValid.title}");
                        if (string.IsNullOrWhiteSpace(itemValid.path))
                        {
                            sBuilder.AppendLine($"Link: https://s.cafef.vn/phan-tich-bao-cao.chn");
                        }
                        else
                        {
                            sBuilder.AppendLine($"Link: {itemValid.path}");
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.CafeF|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }
    }
}
