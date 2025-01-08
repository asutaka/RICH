using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Stock.Service
{
    public interface ITeleService
    {
        Task SendMessage(long id, string mes);
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

                var lRes = await _messageService.HandleMessage(msg.Text);
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
