﻿using MongoDB.Driver;
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
            var lMes = new List<string>();
            try
            {
                var ssi_COM = await SSI(false);
                if (ssi_COM?.Any() ?? false) 
                {
                    lMes.AddRange(ssi_COM);
                }

                var vcbs = await VCBS();
                if (vcbs?.Any() ?? false)
                {
                    lMes.AddRange(vcbs);
                }

                var vci = await VCI();
                if (vci?.Any() ?? false)
                {
                    lMes.AddRange(vci);
                }

                var dsc = await DSC();
                if(dsc?.Any() ?? false)
                {
                    lMes.AddRange(dsc);
                }

                var vinacapital = await VinaCapital();
                if (vinacapital?.Any() ?? false)
                {
                    lMes.AddRange(vinacapital);
                }

                var vndirect_COM = await VNDirect(false);
                if (vndirect_COM?.Any() ?? false)
                {
                    lMes.AddRange(vndirect_COM);
                }

                var mbs_COM = await MBS(false);
                if (mbs_COM?.Any() ?? false)
                {
                    lMes.AddRange(mbs_COM);
                }

                var fpts_COM = await FPTS(false);
                if (fpts_COM?.Any() ?? false)
                {
                    lMes.AddRange(fpts_COM);
                }

                var bsc_COM = await BSC(false);
                if (bsc_COM?.Any() ?? false)
                {
                    lMes.AddRange(bsc_COM);
                }

                var ma = await MigrateAsset();
                if (ma?.Any() ?? false)
                {
                    lMes.AddRange(ma);
                }

                var agribank_COM = await Agribank(false);
                if (agribank_COM?.Any() ?? false)
                {
                    lMes.AddRange(agribank_COM);
                }

                var psi_COM = await PSI(false);
                if (psi_COM?.Any() ?? false)
                {
                    lMes.AddRange(psi_COM);
                }

                var kbs_COM = await KBS(false);
                if (kbs_COM?.Any() ?? false)
                {
                    lMes.AddRange(kbs_COM);
                }


                var ssi_Ins = await SSI(true);
                if (ssi_Ins?.Any() ?? false)
                {
                    lMes.AddRange(ssi_Ins);
                }

                var vndirect_Ins = await VNDirect(true);
                if (vndirect_Ins?.Any() ?? false)
                {
                    lMes.AddRange(vndirect_Ins);
                }

                var mbs_Ins = await MBS(true);
                if (mbs_Ins?.Any() ?? false)
                {
                    lMes.AddRange(mbs_Ins);
                }

                var fpts_Ins = await FPTS(true);
                if (fpts_Ins?.Any() ?? false)
                {
                    lMes.AddRange(fpts_Ins);
                }

                var bsc_Ins = await BSC(true);
                if (bsc_Ins?.Any() ?? false)
                {
                    lMes.AddRange(bsc_Ins);
                }

                var agribank_Ins = await Agribank(true);
                if (agribank_Ins?.Any() ?? false)
                {
                    lMes.AddRange(agribank_Ins);
                }

                var psi_Ins = await PSI(true);
                if (psi_Ins?.Any() ?? false)
                {
                    lMes.AddRange(psi_Ins);
                }

                var kbs_Ins = await KBS(true);
                if (kbs_Ins?.Any() ?? false)
                {
                    lMes.AddRange(kbs_Ins);
                }

                var cafef = await CafeF();
                if (cafef?.Any() ?? false)
                {
                    lMes.AddRange(cafef);
                }

                var count = lMes.Count();
                if (count > 1)
                {
                    var index = 1;
                    foreach (var item in lMes)
                    {
                        sBuilder.AppendLine($"{index++}. {item}");
                    }
                }
                else if(count == 1)
                {
                    sBuilder.Append(lMes.First());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.BaoCaoPhanTich|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<List<string>> DSC()
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");

                var lRes = await _apiService.DSC_GetPost();
                if (lRes is null)
                {
                    return null;
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
                            lMes.Add($"[[DSC - Phân tích cổ phiếu] - {code}:{itemValid.attributes.title}](www.dsc.com.vn/bao-cao-phan-tich/{itemValid.attributes.slug})");
                        }
                        else if (!itemValid.attributes.category_id.data.attributes.slug.Contains("beat"))
                        {
                            lMes.Add($"[DSC - Báo cáo phân tích](www.dsc.com.vn/bao-cao-phan-tich/{itemValid.attributes.slug})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.DSC|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> VinaCapital()
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");

                var lRes = await _apiService.VinaCapital_GetPost();
                if (lRes is null)
                {
                    return null;
                }

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
                                builder.Eq(x => x.ty, (int)ESource.VinaCapital),
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
                            ty = (int)ESource.VinaCapital
                        });

                        lMes.Add($"[[VinaCapital] - {itemValid.title}]({itemValid.path})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VinaCapital|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> VNDirect(bool mode)
        {
            var lMes = new List<string>();
            try
            {
                var lRes = await _apiService.VNDirect_GetPost(mode);
                if (lRes is null)
                    return null;

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

                        var link = itemValid.attachments.Any() ? $"https://www.vndirect.com.vn/cmsupload/beta/{itemValid.attachments.First().name}" : $"{commonlink}";
                        lMes.Add($"[{title} {itemValid.newsTitle}]({link})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VNDirect|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> MigrateAsset()
        {
            var lMes = new List<string>();
            try
            {
                var lRes = await _apiService.MigrateAsset_GetPost();
                if (lRes is null)
                    return null;

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
                            lMes.Add($"[[MigrateAsset - Phân tích cổ phiếu] {itemValid.stock_related}:{itemValid.title}](https://masvn.com/api{itemValid.file_path})");
                        }
                        else
                        {
                            lMes.Add($"[[MigrateAsset - Báo cáo phân tích] {itemValid.title}](https://masvn.com/api{itemValid.file_path})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.MigrateAsset|EXCEPTION| {ex.Message}");
            }
            return lMes;
        }

        private async Task<List<string>> Agribank(bool mode)
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[Agribank - Báo cáo ngành]" : "[Agribank - Phân tích cổ phiếu]";

                var lRes = await _apiService.Agribank_GetPost(mode);
                if (lRes is null)
                    return null;

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
                            lMes.Add($"[{title} {itemValid.Title.Replace("AGR Snapshot", "").Trim()}](https://agriseco.com.vn/Report/ReportFile/{itemValid.ReportID})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.Agribank|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> SSI(bool mode)
        {
            var lMes = new List<string>();
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
                    return null;

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

                        lMes.Add($"[{title} {itemValid.title}]({commonlink})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.SSI|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> VCI()
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lRes = await _apiService.VCI_GetPost();
                if (lRes is null)
                    return null;

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

                        if (string.IsNullOrWhiteSpace(itemValid.file))
                            continue;

                        _bcptRepo.InsertOne(new ConfigBaoCaoPhanTich
                        {
                            d = d,
                            key = itemValid.id.ToString(),
                            ty = (int)ESource.VCI
                        });

                        if (itemValid.pageLink == "company-research")
                        {
                            lMes.Add($"[[VCI - Phân tích cổ phiếu] {itemValid.name}]({itemValid.file})");
                        }
                        else if (itemValid.pageLink == "sector-reports")
                        {
                            lMes.Add($"[[VCI - Báo cáo Ngành] {itemValid.name}]({itemValid.file})");
                        }
                        else
                        {
                            lMes.Add($"[[VCI - Báo cáo vĩ mô] {itemValid.name}]({itemValid.file})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VCI|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> VCBS()
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lRes = await _apiService.VCBS_GetPost();
                if (lRes is null)
                    return null;

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
                            lMes.Add($"[[VCBS - Phân tích cổ phiếu] {itemValid.name}](https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet)");
                        }
                        else if (itemValid.category.code == "BCN")
                        {
                            lMes.Add($"[[VCBS - Báo cáo Ngành] {itemValid.name}](https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet)");
                        }
                        else
                        {
                            lMes.Add($"[[VCBS - Báo cáo vĩ mô] {itemValid.name}](https://www.vcbs.com.vn/trung-tam-phan-tich/bao-cao-chi-tiet)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.VCBS|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> BSC(bool mode)
        {
            var lMes = new List<string>();
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
                    return null;

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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{itemValid.path}";
                        lMes.Add($"[{title} {HttpUtility.HtmlDecode(itemValid.title)}]({link})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.BSC|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> MBS(bool mode)
        {
            var lMes = new List<string>();
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
                    return null;

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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{HttpUtility.HtmlDecode(itemValid.path)}";
                        lMes.Add($"[{title} {HttpUtility.HtmlDecode(itemValid.title)}]({link})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.MBS|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> PSI(bool mode)
        {
            var lMes = new List<string>();
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
                    return null;

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
                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{itemValid.path}";
                        lMes.Add($"[{title} {itemValid.title}]({link})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.PSI|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> FPTS(bool mode)
        {
            var lMes = new List<string>();
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
                    return null;

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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"{commonlink}" : $"{itemValid.path}";
                        lMes.Add($"[{title} {itemValid.title}]({link})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.FPTS|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> KBS(bool mode)
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var title = mode ? "[KBS - Báo cáo ngành]" : "[KBS - Phân tích cổ phiếu]";

                var lRes = await _apiService.KBS_GetPost(mode);
                if (lRes is null)
                    return null;

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

                        lMes.Add($"[{title} {itemValid.title}]({itemValid.path})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.KBS|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }

        private async Task<List<string>> CafeF()
        {
            var lMes = new List<string>();
            try
            {
                var dt = DateTime.Now;
                var time = new DateTime(dt.Year, dt.Month, dt.Day);
                var d = int.Parse($"{time.Year}{time.Month.To2Digit()}{time.Day.To2Digit()}");
                var lRes = await _apiService.CafeF_GetPost();
                if (lRes is null)
                    return null;

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

                        var link = string.IsNullOrWhiteSpace(itemValid.path) ? $"https://s.cafef.vn/phan-tich-bao-cao.chn" : $"{itemValid.path}";
                        lMes.Add($"[[CafeF - Phân tích] {itemValid.title}]({link})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.CafeF|EXCEPTION| {ex.Message}");
            }

            return lMes;
        }
    }
}
