using StockPr.DAL;
using StockPr.Model;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface IFileService
    {
        List<TudoanhPDF> HSX(Stream data);
    }
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }

        public List<TudoanhPDF> HSX(Stream data)
        {
            double posstt = 0;
            double poss = 0;

            double posMuaKL = 0;
            double posBanKL = 0;
            double posMuaGiaTri = 0;
            double posBanGiaTri = 0;
            double posMuaKL_ThoaThuan = 0;
            double posBanKL_ThoaThuan = 0;
            double posMuaGiaTri_ThoaThuan = 0;
            double tempVal = 0;

            using (var document = UglyToad.PdfPig.PdfDocument.Open(data))
            {
                var checkNgay = false;
                var setDate = false;
                int step = 0;

                var lData = new List<TudoanhPDF>();
                var localData = new TudoanhPDF();
                DateTime date = DateTime.MinValue;
                for (int i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                    var mcs = page.GetMarkedContents();

                    foreach (var mc in mcs)
                    {
                        var letters = mc.Letters;
                        var paths = mc.Paths;
                        var images = mc.Images;
                        //
                        if (!letters.Any())
                            continue;
                        var location = letters.ElementAt(0).Location;
                        var word = string.Join("", letters.Select(x => x.Value));
                        word = word.Replace(",", "").Replace(".", "");
                        if (!checkNgay)
                        {
                            if (!word.Contains("Ngày"))
                                continue;
                            checkNgay = true;
                            continue;
                        }

                        if (checkNgay && !setDate)
                        {
                            posstt = location.X;
                            poss = letters.ElementAt(letters.Count - 1).Location.X;

                            date = word.ToDateTime("dd/MM/yyyy");
                            setDate = true;
                            continue;
                        }

                        if (word.Contains("Ngày"))
                        {
                            lData.Add(localData);
                            return lData;
                        }

                        if (!setDate)
                            continue;

                        if (date == DateTime.MinValue)
                        {
                            return null;
                        }

                        if (posMuaGiaTri_ThoaThuan <= 0)
                        {
                            if (tempVal <= 0)
                            {
                                if (!word.Contains("Mua"))
                                    continue;
                                tempVal = location.X;
                                continue;
                            }

                            if (posMuaKL <= 0)
                            {
                                posMuaKL = (location.X + tempVal) / 2;
                            }
                            else if (posBanKL <= 0)
                            {
                                posBanKL = (location.X + tempVal) / 2;
                            }
                            else if (posMuaGiaTri <= 0)
                            {
                                posMuaGiaTri = (location.X + tempVal) / 2;
                            }
                            else if (posBanGiaTri <= 0)
                            {
                                posBanGiaTri = (location.X + tempVal) / 2;
                            }
                            else if (posMuaKL_ThoaThuan <= 0)
                            {
                                posMuaKL_ThoaThuan = (location.X + tempVal) / 2;
                            }
                            else if (posBanKL_ThoaThuan <= 0)
                            {
                                posBanKL_ThoaThuan = (location.X + tempVal) / 2;
                            }
                            else if (posMuaGiaTri_ThoaThuan <= 0)
                            {
                                posMuaGiaTri_ThoaThuan = (location.X + tempVal) / 2;
                            }

                            tempVal = location.X;
                            continue;
                        }

                        if (location.X < posstt)
                        {
                            var isstt = int.TryParse(word, out var stt);
                            if (!isstt)
                            {
                                continue;
                            }
                            step = 1;

                            if (localData.no > 0)
                            {
                                lData.Add(new TudoanhPDF
                                {
                                    no = localData.no,
                                    d = new DateTimeOffset(date, TimeSpan.FromHours(0)).ToUnixTimeSeconds(),
                                    s = localData.s,
                                    bvo = localData.bvo,
                                    svo = localData.svo,
                                    bva = localData.bva,
                                    sva = localData.sva,
                                    bvo_pt = localData.bvo_pt,
                                    svo_pt = localData.svo_pt,
                                    bva_pt = localData.bva_pt,
                                    sva_pt = localData.sva_pt,
                                    t = DateTimeOffset.Now.ToUnixTimeSeconds()
                                });
                                localData.s = string.Empty;
                                localData.bvo = 0;
                                localData.svo = 0;
                                localData.bva = 0;
                                localData.sva = 0;
                                localData.bvo_pt = 0;
                                localData.svo_pt = 0;
                                localData.bva_pt = 0;
                                localData.sva_pt = 0;
                            }

                            localData.no = stt;
                            continue;
                        }
                        else if (location.X >= posstt
                            && location.X < poss
                            && step == 1)
                        {
                            step = 2;
                            localData.s = word;
                        }
                        else if (location.X >= poss
                            && location.X < posMuaKL
                            && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.bvo = val;
                        }
                        else if (location.X >= posMuaKL
                            && location.X < posBanKL
                            && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.svo = val;
                        }
                        else if (location.X >= posBanKL
                            && location.X < posMuaGiaTri
                            && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.bva = val;
                        }
                        else if (location.X >= posMuaGiaTri
                            && location.X < posBanGiaTri
                            && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.sva = val;
                        }
                        else if (location.X >= posBanGiaTri
                            && location.X < posMuaKL_ThoaThuan
                            && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.bvo_pt = val;
                        }
                        else if (location.X >= posMuaKL_ThoaThuan
                           && location.X < posBanKL_ThoaThuan
                           && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.svo_pt = val;
                        }
                        else if (location.X >= posBanKL_ThoaThuan
                          && location.X < posMuaGiaTri_ThoaThuan
                          && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.bva_pt = val;
                        }
                        else if (location.X >= posMuaGiaTri_ThoaThuan
                            && step == 2)
                        {
                            var isValid = int.TryParse(word, out var val);
                            if (isValid)
                                localData.sva_pt = val;
                        }
                    }
                }

                return lData;
            }
        }
    }
}
