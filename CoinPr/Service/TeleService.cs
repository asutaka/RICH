using CoinPr.DAL;
using CoinPr.DAL.Entity;
using CoinPr.Utils;
using MongoDB.Driver;
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
        private readonly ITokenUnlockRepo _tokenUnlockRepo;
        private readonly IConfiguration _config;
        private TelegramBotClient _bot;
        private Client _client;
        private const long _idUser = 1066022551;
        public TeleService(ILogger<TeleService> logger,
            IMessageService messageService,
            ISignalRepo signalRepo,
            ITokenUnlockRepo tokenUnlockRepo,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _messageService = messageService;
            _signalRepo = signalRepo;
            _tokenUnlockRepo = tokenUnlockRepo;
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
                    if (val.message.Peer.ID == 2088766055) //Token Unlocks
                    {
                        try
                        {
                            var dt = DateTime.Now;
                            var content = val.message.ToString();
                            var indexDays = content.IndexOf("days");
                            var indexOf = content.IndexOf("#");
                            var indexOfDate = content.IndexOf("Date");
                            var indexOfAmount = content.IndexOf("Amount");
                            if (indexOfDate <= 0 || indexOfAmount <= 0 || indexDays <= 0 || indexOf <= 0
                                || indexOf <= indexDays)
                                continue;

                            var dayStr = content.Substring(indexOfDate, indexOfAmount - indexOfDate)?.Replace("Date", "").Trim()?.Split(' ')?.FirstOrDefault();
                            if (string.IsNullOrWhiteSpace(dayStr))
                                continue;
                            var arrDay = dayStr.Split('/');
                            if (arrDay.Length != 3)
                                continue;

                            var time = new DateTimeOffset(int.Parse(arrDay[2]), int.Parse(arrDay[0]), int.Parse(arrDay[1]), 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                            var s = content.Substring(indexDays + 4, indexOf - (indexDays + 4)).Trim();
                            var entity = new TokenUnlock
                            {
                                s = s,
                                time = (int)time
                            };

                            FilterDefinition<TokenUnlock> filter = null;
                            var builder = Builders<TokenUnlock>.Filter;
                            var lFilter = new List<FilterDefinition<TokenUnlock>>()
                            {
                                builder.Eq(x => x.s, entity.s),
                                builder.Eq(x => x.time, entity.time),
                            };
                            foreach (var item in lFilter)
                            {
                                if (filter is null)
                                {
                                    filter = item;
                                    continue;
                                }
                                filter &= item;
                            }

                            var exist = _tokenUnlockRepo.GetEntityByFilter(filter);
                            if (exist != null)
                                continue;

                            _tokenUnlockRepo.InsertOne(entity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"TeleService.Client_OnUpdates|EXCEPTION| {ex.Message}");
                        }
                    }
                    //if (StaticVal._dicChannel.Any(x => x.Key == val.message.Peer.ID))
                    //{
                    //    _signalRepo.InsertOne(new DAL.Entity.Signal
                    //    {
                    //        Date = DateTime.Now,
                    //        Channel = val.message.Peer.ID,
                    //        Content = val.message.ToString()
                    //    });
                    //}
                }
            }    
        }
    }
}
