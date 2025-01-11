using StockPr.DAL;
using StockPr.Model;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface IFileService
    {
        List<TudoanhPDF> HSX(Stream data);
        List<TudoanhPDF> HNX(Stream data);
    }
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IStockRepo _stockRepo;
        public FileService(ILogger<FileService> logger,
                        IStockRepo stockRepo)
        {
            _logger = logger;
            _stockRepo = stockRepo;
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

        public List<TudoanhPDF> HNX(Stream data)
        {
            var content = pdfText(data);
            return MapHNX(content);
        }

        private string pdfText(Stream data)
        {
            //try
            //{
            //    var reader = new iTextSharp.text.pdf.PdfReader(data);

            //    string text = string.Empty;
            //    for (int page = 1; page <= reader.NumberOfPages; page++)
            //    {
            //        text += iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page);
            //        text += "\n";
            //    }

            //    reader.Close();
            //    return text;
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex.Message);
            //}
            return string.Empty;
        }

        private List<TudoanhPDF> MapHNX(string content)
        {
            var indexFileHNX = content.IndexOf("Sở Giao dịch Chứng khoán Hà Nội");
            if (indexFileHNX < 0)
                return null;

            var strTieuDe = "GIAO DỊCH TỰ DOANH THEO MÃ CHỨNG KHOÁN";
            var indexNgay = content.IndexOf(strTieuDe);
            if (indexNgay < 0)
                return null;
            content = content.Substring(indexNgay);
            var lLine = content.Split(new string[] { "\n" }, StringSplitOptions.None);
            var dateStr = lLine.ElementAt(0).Replace(strTieuDe, "").Trim();
            var date = dateStr.ToDateTime("dd/MM/yyyy");

            var lStock = _stockRepo.GetAll();

            var isRead = false;
            var isBegin = false;
            var lData = new List<TudoanhPDF>();
            foreach (var item in lLine)
            {
                try
                {
                    if (item.Contains("Tổng GTGD Tự doanh"))
                    {
                        isRead = true;
                    }

                    if (!isRead)
                        continue;

                    var arrData = item.Split(new string[] { " " }, StringSplitOptions.None);
                    if (!isBegin
                        && arrData.ElementAt(0) == "1")
                    {
                        isBegin = true;
                    }
                    if (!isBegin)
                        continue;
                    var count = arrData.Length;
                    if (count < 8)
                        continue;

                    var maCKCheck = arrData.ElementAt(1).Trim();
                    var check = lStock.Any(x => x.s == maCKCheck);
                    if (check)
                    {
                        var model = new TudoanhPDF
                        {
                            no = int.Parse(arrData.ElementAt(0)),
                            d = new DateTimeOffset(date, TimeSpan.FromHours(0)).ToUnixTimeSeconds(),
                            s = arrData.ElementAt(1),
                            bvo = int.Parse(arrData.ElementAt(2).Replace(",", "").Replace(".", "")),
                            bva = (int)(long.Parse(arrData.ElementAt(3).Replace(",", "").Replace(".", "")) / 1000),
                            svo = int.Parse(arrData.ElementAt(4).Replace(",", "").Replace(".", "")),
                            sva = (int)(long.Parse(arrData.ElementAt(5).Replace(",", "").Replace(".", "")) / 1000),
                            t = DateTimeOffset.Now.ToUnixTimeSeconds()
                        };
                        lData.Add(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            return lData;
        }
    }
}
