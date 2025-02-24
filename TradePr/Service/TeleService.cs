using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace TradePr.Service
{
    public interface ITeleService
    {
        Task SendMessage(long id, string mes);

    }
    public class TeleService : ITeleService
    {
        private readonly ILogger<TeleService> _logger;
        private TelegramBotClient _bot = new TelegramBotClient("7783423206:AAGnpRM_8xnxr44sbgOX-ktIHXQEsyMxH6A");
        public TeleService(ILogger<TeleService> logger)
        {
            _logger = logger;
            _bot.OnMessage += OnMessage;
        }
        async Task OnMessage(Message msg, UpdateType type)
        {
            try
            {
               // if (string.IsNullOrWhiteSpace(msg.Text)
               //|| type != UpdateType.Message) return;

               // var lRes = await _messageService.ReceivedMessage(msg);
               // if (lRes?.Any() ?? false)
               // {
               //     foreach (var item in lRes)
               //     {
               //         if (!string.IsNullOrWhiteSpace(item.Message))
               //         {
               //             await _bot.SendMessage(msg.Chat, item.Message);
               //             Thread.Sleep(200);
               //         }
               //         if (item.Stream != null && item.Stream.Content.Length > 0)
               //         {
               //             await _bot.SendPhoto(msg.Chat, item.Stream);
               //             Thread.Sleep(200);
               //         }
               //     }
               // }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeleService.OnMessage|EXCEPTION| {ex.Message}");
            }
        }

        public async Task SendMessage(long id, string mes)
        {
            try
            {
                await _bot.SendMessage(id, mes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeleService.SendMessage|EXCEPTION| {ex.Message}");
            }
        }
    }
}
