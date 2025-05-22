using TradePr.Utils;

namespace TradePr.Model
{
    public class clsTerminal
    {
        public EKey Exchange { get; set; }//Binance/Bybit
        public EKey? Position { get; set; }//Long/Short
        public EKey? Terminal { get; set; }//Start/Stop
        public EKey? Thread { get; set; }//Số lệnh tối đa trong một thời điểm
        public EKey? Max { get; set; }//Giá trị lệnh 
        public EKey? Action { get; set; }//Add/Remove
        public bool Balance { get; set; }//Balance
        public string Coin { get; set; }
    }
}
