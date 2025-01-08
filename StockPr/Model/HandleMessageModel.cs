using Telegram.Bot.Types;

namespace StockPr.Model
{
    public class HandleMessageModel
    {
        public string Message { get; set; }
        public InputFileStream Stream { get; set; }
    }
}
