using StockPr.DAL.Entity;
using System.Globalization;

namespace StockPr.Utils
{
    public static class ExtensionMethod
    {
        public static string RemoveSpace(this string val)
        {
            return val.Replace(" ", "").Replace(",", "").Replace(".", "").Replace("-", "").Replace("_", "").Trim();
        }

        public static long GetYoyQuarter(this int time)
        {
            var year = time / 10;
            var quarter = time - year * 10;
            return int.Parse($"{year - 1}{quarter}");
        }

        public static string GetNameQuarter(this int time)
        {
            var year = time / 10;
            var quarter = time - year * 10;
            return $"{quarter.To2Digit()}/{year - 2000}";
        }

        public static long GetPrevQuarter(this int time)
        {
            var year = time / 10;
            var quarter = time - year * 10;
            if (quarter == 1)
            {
                year--;
                quarter = 4;
            }
            else
            {
                quarter--;
            }
            return int.Parse($"{year}{quarter}");
        }

        public static string GetNameMonth(this int time)
        {
            var year = time / 100;
            var month = time - year * 100;
            return $"{month.To2Digit()}/{year - 2000}";
        }

        public static DateTime ToDateTime(this string val, string format)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val))
                    return DateTime.MinValue;
                DateTime dt = DateTime.ParseExact(val, format, CultureInfo.InvariantCulture);
                return dt;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        public static string To2Digit(this int val)
        {
            if (val > 9)
                return val.ToString();
            return $"0{val}";
        }

        public static bool IsTonKho(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.XayDung,
                (int)EStockType.KCN,
                (int)EStockType.BDS,
                (int)EStockType.Thep,
                (int)EStockType.BanLe,
                (int)EStockType.Oto,
                (int)EStockType.OtoTai,
                (int)EStockType.PhanBon,
                (int)EStockType.Than,
                (int)EStockType.XiMang,
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsFDI(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.KCN
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsNguoiMua(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.KCN,
                (int)EStockType.BDS
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsXNK(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.Thep,
                (int)EStockType.Than,
                (int)EStockType.ThuySan,
                (int)EStockType.PhanBon,
                (int)EStockType.HoaChat,
                (int)EStockType.Go,
                (int)EStockType.Gao,
                (int)EStockType.XiMang,
                (int)EStockType.CaPhe,
                (int)EStockType.CaoSu,
                (int)EStockType.Oto,
                (int)EStockType.OtoTai,
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsBanLe(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.BanLe
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsHangKhong(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.HangKhong
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsCaoSu(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.CaoSu
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsNganHang(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.NganHang
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }

        public static bool IsChungKhoan(this Stock stock)
        {
            if (stock.cat is null)
                return false;

            var lCat = new List<int>
            {
                (int)EStockType.ChungKhoan
            };
            foreach (var item in lCat)
            {
                if (stock.cat.Any(x => x.ty == item))
                    return true;
            }

            return false;
        }
    }
}
