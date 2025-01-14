using Telegram.Bot.Types;

namespace CoinPr.Model
{
    public class HandleMessageModel
    {
        public string Message { get; set; }
        public InputFileStream Stream { get; set; }
    }
}
