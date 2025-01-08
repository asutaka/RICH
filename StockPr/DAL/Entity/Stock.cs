using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Stock : BaseDTO
    {
        public string s { get; set; }//symbol
        public int ex { get; set; }//exchange
        public CompanyProfileDataModel p { get; set; } //profile
        public int rank { get; set; }
        public List<CategoryType> cat { get; set; }//Nhóm ngành và tỉ lệ đóng góp của nhóm ngành
        public List<int> f { get; set; }//follow: Giá những mặt hàng cần quan tâm
        public List<IndicatorModel> indicator { get; set; }
        public int status { get; set; }
    }

    public class CompanyProfileDataModel
    {
        public string fd { get; set; }//Năm thành lập(foundingDate)
        public double cc { get; set; }//Vốn điều lệ(charterCapital)
        public int noe { get; set; }//Số lượng nhân viên(numberOfEmployee)
        public string ld { get; set; }//Ngày niêm yết(listingDate)
        public double fp { get; set; }//Giá chào sàn(firstPrice)
        public double iss { get; set; }//KL đang niêm yết(issueShare)
        public double lv { get; set; }//Vốn hóa(listedValue)
        public string n { get; set; }//companyName
        public double q { get; set; }//SLCP lưu hành(quantity)
    }

    public class IndicatorModel
    {
        public int type { get; set; }//Loại chỉ báo
        public int rank { get; set; }//Thứ tự xếp hạng của chỉ báo
        public double avg_rate { get; set; }//Trung bình mỗi lần vào lệnh
        public int avg_num { get; set; }//Số nến nắm giữ trung bình
        public double win_rate { get; set; }//Tỉ lệ winrate
        public double loss_rate { get; set; }//Tỉ lệ lossrate
    }

    public class CategoryType
    {
        public int ty { get; set; }
        public int ty_r { get; set; }
    }
}
