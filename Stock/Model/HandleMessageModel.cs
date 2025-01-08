using Telegram.Bot.Types;

namespace Stock.Model
{
    public class HandleMessageModel
    {
        public string Message { get; set; }
        public InputFileStream Stream { get; set; }
    }
}
