using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System.Web;

namespace StockPr.Service
{
    public interface IBaoCaoPhanTichService
    {
        Task<(List<BaoCaoPhanTichModel>, List<string>)> BaoCaoPhanTich();
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

        public async Task<(List<BaoCaoPhanTichModel>, List<string>)> BaoCaoPhanTich()
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            var lError = new List<string>();
            try
            {
                var ssi_COM = await SSI(false);
                if (!ssi_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích SSI");
                }
                if (ssi_COM.Item2?.Any() ?? false) 
                {
                    lMes.AddRange(ssi_COM.Item2);
                }

                var vcbs = await VCBS();
                if (!vcbs.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích VCBS");
                }
                if (vcbs.Item2?.Any() ?? false)
                {
                    lMes.AddRange(vcbs.Item2);
                }

                var vci = await VCI();
                if (!vci.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích VCI");
                }
                if (vci.Item2?.Any() ?? false)
                {
                    lMes.AddRange(vci.Item2);
                }

                var dsc = await DSC();
                if (!dsc.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích DSC");
                }
                if (dsc.Item2?.Any() ?? false)
                {
                    lMes.AddRange(dsc.Item2);
                }

                var vndirect_COM = await VNDirect(false);
                if (!vndirect_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích VNDirect");
                }
                if (vndirect_COM.Item2?.Any() ?? false)
                {
                    lMes.AddRange(vndirect_COM.Item2);
                }

                var mbs_COM = await MBS(false);
                if (!mbs_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích MBS");
                }
                if (mbs_COM.Item2?.Any() ?? false)
                {
                    lMes.AddRange(mbs_COM.Item2);
                }

                //var fpts_COM = await FPTS(false);
                //if (!fpts_COM.Item1)
                //{
                //    lError.Add("[ERROR] Báo cáo phân tích FPTS");
                //}
                //if (fpts_COM.Item2?.Any() ?? false)
                //{
                //    lMes.AddRange(fpts_COM.Item2);
                //}

                var bsc_COM = await BSC(false);
                if (!bsc_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích BSC");
                }
                if (bsc_COM.Item2?.Any() ?? false)
                {
                    lMes.AddRange(bsc_COM.Item2);
                }

                var ma = await MigrateAsset();
                if (!ma.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích MigrateAsset");
                }
                if (ma.Item2?.Any() ?? false)
                {
                    lMes.AddRange(ma.Item2);
                }

                var agribank_COM = await Agribank(false);
                if (!agribank_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích Agribank");
                }
                if (agribank_COM.Item2?.Any() ?? false)
                {
                    lMes.AddRange(agribank_COM.Item2);
                }

                var psi_COM = await PSI(false);
                if (!psi_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích PSI");
                }
                if (psi_COM.Item2?.Any() ?? false)
                {
                    lMes.AddRange(psi_COM.Item2);
                }

                var kbs_COM = await KBS(false);
                if (!kbs_COM.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích KBS");
                }
                if (kbs_COM.Item2?.Any() ?? false)
                {
                    lMes.AddRange(kbs_COM.Item2);
                }

                var ssi_Ins = await SSI(true);
                if (ssi_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(ssi_Ins.Item2);
                }

                var vndirect_Ins = await VNDirect(true);
                if (vndirect_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(vndirect_Ins.Item2);
                }

                var mbs_Ins = await MBS(true);
                if (mbs_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(mbs_Ins.Item2);
                }

                //var fpts_Ins = await FPTS(true);
                //if (fpts_Ins.Item2?.Any() ?? false)
                //{
                //    lMes.AddRange(fpts_Ins.Item2);
                //}

                var bsc_Ins = await BSC(true);
                if (bsc_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(bsc_Ins.Item2);
                }

                var agribank_Ins = await Agribank(true);
                if (agribank_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(agribank_Ins.Item2);
                }

                var psi_Ins = await PSI(true);
                if (psi_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(psi_Ins.Item2);
                }

                var kbs_Ins = await KBS(true);
                if (kbs_Ins.Item2?.Any() ?? false)
                {
                    lMes.AddRange(kbs_Ins.Item2);
                }

                var cafef = await CafeF();
                if (!cafef.Item1)
                {
                    lError.Add("[ERROR] Báo cáo phân tích CafeF");
                }
                if (cafef.Item2?.Any() ?? false)
                {
                    lMes.AddRange(cafef.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.BaoCaoPhanTich|EXCEPTION| {ex.Message}");
            }
            lError.Clear();

            return (lMes, lError);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> DSC()
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");

                var res = await _apiService.DSC_GetPost();
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.attributes.public_at > time);
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

                        var title = string.Empty;
                        if (itemValid.attributes.category_id.data.attributes.slug.Equals("phan-tich-doanh-nghiep"))
                        {
                            var code = itemValid.attributes.slug.Split('-').First().ToUpper();
                            title = $"|DSC - Cổ phiếu {code}| {itemValid.attributes.title.Replace("[","").Replace("]", "").Replace("(", "").Replace(")", "")}";
                        }
                        else if (!itemValid.attributes.category_id.data.attributes.slug.Contains("beat"))
                        {
                            title = $"|DSC - Báo cáo phân tích| {itemValid.attributes.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}";
                        }

                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            lMes.Add(new BaoCaoPhanTichModel
                            {
                                content = title,
                                link = $"www.dsc.com.vn/bao-cao-phan-tich/{itemValid.attributes.slug}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.DSC|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> VNDirect(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var res = await _apiService.VNDirect_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|VNDirect - Báo cáo ngành|" : "|VNDirect - Cổ phiếu|";
                var commonlink = mode ? "https://dstock.vndirect.com.vn/trung-tam-phan-tich/bao-cao-nganh" 
                                    : "https://dstock.vndirect.com.vn/trung-tam-phan-tich/bao-cao-phan-tich-dn";

                var t = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lValid = res.Item2.Where(x => int.Parse(x.newsDate.Replace("-", "")) >= t);
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

                        var link = itemValid.attachments.Any() ? $"https://www.vndirect.com.vn/cmsupload/beta/{itemValid.attachments.First().name}" : $"{commonlink}";
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {itemValid.newsTitle.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                            link = link
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VNDirect|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> MigrateAsset()
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var res = await _apiService.MigrateAsset_GetPost();
                if (!res.Item1)
                {
                    return (false, null);
                }

                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");

                var lValid = res.Item2.Where(x => x.published_at > time);
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

                        var title = $"|MigrateAsset - Báo cáo phân tích| {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}";
                        if (itemValid.stock_related.Length == 3)
                        {
                            title = $"|MigrateAsset - Cổ phiếu {itemValid.stock_related}| {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}";
                        }
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = title,
                            link = $"https://masvn.com/api{itemValid.file_path}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.MigrateAsset|EXCEPTION| {ex.Message}");
            }
            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> Agribank(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|Agribank - Báo cáo ngành|" : "|Agribank - Phân tích cổ phiếu|";

                var res = await _apiService.Agribank_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.Date > time);
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
                            lMes.Add(new BaoCaoPhanTichModel
                            {
                                content = $"{title} {itemValid.Title.Replace("AGR Snapshot", "").Trim().Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                                link = $"https://agriseco.com.vn/Report/ReportFile/{itemValid.ReportID}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.Agribank|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> SSI(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|SSI - Báo cáo ngành|" : "|SSI - Cổ phiếu|";
                var commonlink = mode ? "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-nganh"
                                    : "https://www.ssi.com.vn/khach-hang-ca-nhan/bao-cao-cong-ty";

                var res = await _apiService.SSI_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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

                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                            link = commonlink
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.SSI|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> VCI()
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var res = await _apiService.VCI_GetPost();
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.makerDate >= time
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

                        if (string.IsNullOrWhiteSpace(itemValid.file))
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id.ToString(),
                            ty = (int)ESource.VCI
                        });

                        var title = $"|VCI - Báo cáo vĩ mô| {itemValid.name}";
                        if (itemValid.pageLink == "company-research")
                        {
                            title = $"|VCI - Cổ phiếu| {itemValid.name}";
                        }
                        else if (itemValid.pageLink == "sector-reports")
                        {
                            title = $"|VCI - Báo cáo Ngành| {itemValid.name}";
                        }

                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", ""),
                            link = itemValid.file
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VCI|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> VCBS()
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var res = await _apiService.VCBS_GetPost();
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.publishedAt >= time
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
                            lMes.Add(new BaoCaoPhanTichModel
                            {
                                content = $"|VCBS - Cổ phiếu| {itemValid.name.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                                link = "https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet?code=BCDN"
                            });
                        }
                        else if (itemValid.category.code == "BCN")
                        {
                            lMes.Add(new BaoCaoPhanTichModel
                            {
                                content = $"|VCBS - Báo cáo Ngành| {itemValid.name.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                                link = "https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet?code=BCN"
                            });
                        }
                        else
                        {
                            lMes.Add(new BaoCaoPhanTichModel
                            {
                                content = $"|VCBS - Báo cáo vĩ mô| {itemValid.name.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                                link = "https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet?code=BCVM"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VCBS|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> BSC(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|BSC - Báo cáo ngành|" : "|BSC - Cổ phiếu|";
                var commonlink = mode ? "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/2"
                                    : "https://www.bsc.com.vn/bao-cao-phan-tich/danh-muc-bao-cao/1";

                var res = await _apiService.BSC_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{itemValid.path}";
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {HttpUtility.HtmlDecode(itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", ""))}",
                            link = link
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.BSC|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> MBS(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|MBS - Báo cáo ngành|" : "|MBS - Cổ phiếu|";
                var commonlink = mode ? "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/bao-cao-phan-tich-nganh/"
                                    : "https://mbs.com.vn/trung-tam-nghien-cuu/bao-cao-phan-tich/nghien-cuu-co-phieu/";

                var res = await _apiService.MBS_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{HttpUtility.HtmlDecode(itemValid.path)}";
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {HttpUtility.HtmlDecode(itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", ""))}",
                            link = link
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.MBS|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> PSI(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|PSI - Báo cáo ngành|" : "|PSI - Cổ phiếu|";
                var commonlink = mode ? "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-nganh"
                                    : "https://www.psi.vn/vi/trung-tam-phan-tich/bao-cao-phan-tich-doanh-nghiep";

                var res = await _apiService.PSI_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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
                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{itemValid.path}";
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                            link = link
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.PSI|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> FPTS(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|FPTS - Báo cáo ngành|" : "|FPTS - Cổ phiếu|";
                var commonlink = mode ? "https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=174"
                                    : "https://ezsearch.fpts.com.vn/Services/EzReport/?tabid=179";

                var res = await _apiService.FPTS_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{itemValid.path}";
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                            link = link
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.FPTS|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> KBS(bool mode)
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "|KBS - Báo cáo ngành|" : "|KBS - Cổ phiếu|";

                var res = await _apiService.KBS_GetPost(mode);
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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

                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"{title} {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                            link = itemValid.path.Replace("(", "").Replace(")", "").Replace(" ", "")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.KBS|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }

        private async Task<(bool, List<BaoCaoPhanTichModel>)> CafeF()
        {
            var lMes = new List<BaoCaoPhanTichModel>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var res = await _apiService.CafeF_GetPost();
                if (!res.Item1)
                {
                    return (false, null);
                }

                var lValid = res.Item2.Where(x => x.date >= time);
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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"https://s.cafef.vn/phan-tich-bao-cao.chn" : $"{itemValid.path}";
                        lMes.Add(new BaoCaoPhanTichModel
                        {
                            content = $"|CafeF - Phân tích| {itemValid.title.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "")}",
                            link = link
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.CafeF|EXCEPTION| {ex.Message}");
            }

            return (true, lMes);
        }
    }
}
