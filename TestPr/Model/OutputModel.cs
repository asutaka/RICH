namespace TestPr.Model
{
    public class OutputModel
    {
        public DateTime BuyTime { get; set; }
        public DateTime SellTime { get; set; }
        public string ViThe { get; set; }//Long, Short
        public decimal StartPrice { get; set; }
        public decimal EndPrice { get; set; }
        public int NamGiu { get; set; }//nắm giữ bao nhiêu nến
        public decimal TiLe { get; set; }
        public decimal TienLaiThucTe { get; set; }
        public string LaiLo { get; set; }//Lãi, Lỗ
    }
}
