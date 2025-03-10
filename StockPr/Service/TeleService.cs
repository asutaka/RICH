using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockPr.Service
{
    public interface ITeleService
    {
        Task SendMessage(long id, string mes, Dictionary<string, string> dLink = null);
        Task SendMessage(long id, string mes, string link);
        Task SendPhoto(long id, InputFileStream stream);

    }
    public class TeleService : ITeleService
    {
        private readonly ILogger<TeleService> _logger;
        private readonly IMessageService _messageService;
        private TelegramBotClient _bot = new TelegramBotClient("7422438658:AAEPzAwq-5rA-5dLEFRpPrdOBt9yMfnkBxA");
        public TeleService(ILogger<TeleService> logger,
            IMessageService messageService)
        {
            _logger = logger;
            _messageService = messageService;
            _bot.OnMessage += OnMessage;
        }
        async Task OnMessage(Message msg, UpdateType type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msg.Text)
               || type != UpdateType.Message) return;

                var lRes = await _messageService.ReceivedMessage(msg);
                if (lRes?.Any() ?? false)
                {
                    foreach (var item in lRes)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Message))
                        {
                            await _bot.SendMessage(msg.Chat, item.Message);
                            Thread.Sleep(200);
                        }
                        if (item.Stream != null && item.Stream.Content.Length > 0)
                        {
                            await _bot.SendPhoto(msg.Chat, item.Stream);
                            Thread.Sleep(200);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeleService.OnMessage|EXCEPTION| {ex.Message}");
            }
        }

        public async Task SendMessage(long id, string mes, string link)
        {
            try
            {
                var message = $"[{mes}]({link})";
                await _bot.SendMessage(id, message, ParseMode.Markdown, linkPreviewOptions: new LinkPreviewOptions
                {
                    IsDisabled = true,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeleService.SendMessage|EXCEPTION| {ex.Message}");
            }
        }

        public async Task SendMessage(long id, string mes, Dictionary<string, string> dLink = null)
        {
            try
            {
                InlineKeyboardMarkup inline = null;
                if (dLink?.Any() ?? false)
                {
                    var lInlineKeyboard = new List<InlineKeyboardButton>();
                    foreach (var item in dLink)
                    {
                        lInlineKeyboard.Add(InlineKeyboardButton.WithUrl(item.Key, item.Value)); 
                    }
                    inline = new InlineKeyboardMarkup(lInlineKeyboard);
                }

                await _bot.SendMessage(id, mes, ParseMode.Markdown, replyMarkup: inline, linkPreviewOptions: new LinkPreviewOptions
                {
                    IsDisabled = true,
                });
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"TeleService.SendMessage|EXCEPTION| {ex.Message}");
            }
        }

        public async Task SendPhoto(long id, InputFileStream stream)
        {
            try
            {
                await _bot.SendPhoto(id, stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeleService.SendPhoto|EXCEPTION| {ex.Message}");
            }
        }
    }
}
