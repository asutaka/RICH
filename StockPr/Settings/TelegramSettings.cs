namespace StockPr.Settings
{
    /// <summary>
    /// Telegram bot configuration
    /// </summary>
    public class TelegramSettings
    {
        public string BotToken { get; set; } = string.Empty;
        public long GroupId { get; set; }
        public long GroupF319Id { get; set; }
        public long ChannelId { get; set; }
        public long ChannelNewsId { get; set; }
        public long UserId { get; set; }
    }
}
