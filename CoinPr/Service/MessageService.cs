using CoinPr.DAL;
using CoinPr.DAL.Entity;
using CoinPr.Model;
using CoinPr.Utils;
using Telegram.Bot.Types;

namespace CoinPr.Service
{
    public interface IMessageService
    {
        Task<List<HandleMessageModel>> ReceivedMessage(Message msg);
    }
    public class MessageService : IMessageService
    {
        private readonly ILogger<MessageService> _logger;
        private static List<UserMessage> _lUserMes = new List<UserMessage>();
        private readonly IUserMessageRepo _userMessageRepo;

        private readonly int _ty = (int)EUserMessageType.CoinPr;
        public MessageService(ILogger<MessageService> logger, IUserMessageRepo userMessageRepo)
        {
            _logger = logger;
            _userMessageRepo = userMessageRepo;
        }

        public async Task<List<HandleMessageModel>> ReceivedMessage(Message msg)
        {
            var lRes = new List<HandleMessageModel>();
            try
            {
                #region Lưu trữ thời gian mới nhất của từng User 
                var time = new DateTimeOffset(msg.Date, TimeSpan.Zero).ToUnixTimeSeconds();
                var entityUserMes = _lUserMes.FirstOrDefault(x => x.u == msg.Chat.Id && x.ty == _ty);
                if (entityUserMes != null)
                {
                    if (entityUserMes.t >= time)
                        return null;

                    entityUserMes.t = time;
                    _userMessageRepo.Update(entityUserMes);
                }
                else
                {
                    var entityMes = new UserMessage
                    {
                        u = msg.Chat.Id,
                        ty = _ty,
                        t = time
                    };
                    _lUserMes.Add(entityMes);
                    _userMessageRepo.InsertOne(entityMes);
                }
                #endregion

                //lRes.AddRange(await HandleMessage(msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.ReceivedMessage|EXCEPTION| {ex.Message}");
            }
            return lRes;
        }
    }
}
