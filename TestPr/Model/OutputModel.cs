namespace TestPr.Model
{
    public class OutputModel
    {
        public DateTime SignalTime { get; set; }
        public DateTime BuyTime { get; set; }
        public DateTime SellTime { get; set; }
        public string ViThe { get; set; }//Long, Short
        public decimal TiLe { get; set; }
        public decimal TP { get; set; }
        public decimal SL { get; set; }

        public decimal StartPrice { get; set; }
        public decimal EndPrice { get; set; }
        public decimal TienLaiThucTe { get; set; }
        
       

        
        public string LaiLo { get; set; }//Lãi, Lỗ
        public int NamGiu { get; set; }//nắm giữ bao nhiêu nến
    }
}
