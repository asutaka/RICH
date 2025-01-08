using Stock.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Stock.Service
{
    public interface IMessageService
    {
        Task<List<HandleMessageModel>> HandleMessage(string mes);
    }
    public class MessageService : IMessageService
    {
        private readonly ILogger<MessageService> _logger;
        public MessageService(ILogger<MessageService> logger)
        {
            _logger = logger;
        }
        public async Task<List<HandleMessageModel>> HandleMessage(string mes)
        {
            var lRes = new List<HandleMessageModel>();
            try
            {
                lRes.Add(new HandleMessageModel
                {
                    Message = "hello"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return lRes;
        }
    }
}
