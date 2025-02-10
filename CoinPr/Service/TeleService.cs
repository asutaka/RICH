using CoinPr.DAL;
using CoinPr.Utils;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TL;
using WTelegram;

namespace CoinPr.Service
{
    public interface ITeleService
    {
        Task SendMessage(long id, string mes);
    }
    public class TeleService : ITeleService
    {
        private readonly ILogger<TeleService> _logger;
        private readonly IMessageService _messageService;
        private readonly ISignalRepo _signalRepo;
        private readonly IConfiguration _config;
        private TelegramBotClient _bot;
        private Client _client;
        private const long _idUser = 1066022551;
        public TeleService(ILogger<TeleService> logger,
            IMessageService messageService,
            ISignalRepo signalRepo,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _messageService = messageService;
            _signalRepo = signalRepo;
            _bot = new TelegramBotClient(config["Telegram:bot"]);
            _bot.OnMessage += OnMessage;
            InitSession().GetAwaiter().GetResult();
        }
        async Task OnMessage(Telegram.Bot.Types.Message msg, UpdateType type)
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

        private async Task InitSession()
        {
            try
            {
                _client = new WTelegram.Client(Config);
                _client.OnUpdates += Client_OnUpdates;
                await _client.LoginUserIfNeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TeleService.InitSession|EXCEPTION| {ex.Message}");
            }
        }

        private string Config(string what)
        {
            switch (what)
            {
                case "api_id": return _config["Telegram:api_id"];
                case "api_hash": return _config["Telegram:api_hash"];
                case "phone_number": return _config["Telegram:phone"];
                case "verification_code": return _config["Telegram:code"];
                default: return null;                  // let WTelegramClient decide the default config
            }
        }

        private async Task Client_OnUpdates(UpdatesBase updates)
        {
            foreach (var update in updates.UpdateList)
            {
                if (update.GetType().Name.Equals("UpdateNewChannelMessage", StringComparison.OrdinalIgnoreCase))
                {
                    var val = update as UpdateNewChannelMessage;
                    if (StaticVal._dicChannel.Any(x => x.Key == val.message.Peer.ID))
                    {
                        _signalRepo.InsertOne(new DAL.Entity.Signal
                        {
                            Date = DateTime.Now,
                            Channel = val.message.Peer.ID,
                            Content = val.message.ToString()
                        });
                        await SendMessage(_idUser, $"{val.message}");
                        continue;
                    }

                    //await SendMessage(1066022551, $"{val.message.Peer.ID}|{val.message}");
                    //Console.WriteLine($"{val.message.Peer.ID}|{val.message}");
                    //continue;
                }
                //Console.WriteLine(update.GetType().Name);
            }    
        }
    }
}
