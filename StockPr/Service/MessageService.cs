﻿using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using Telegram.Bot.Types;

namespace StockPr.Service
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
        private readonly IChartService _chartService;

        private readonly int _ty = (int)EUserMessageType.StockPr;
        public MessageService(ILogger<MessageService> logger, IChartService chartService, IUserMessageRepo userMessageRepo)
        {
            _logger = logger;
            _chartService = chartService;
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

                lRes.AddRange(await HandleMessage(msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.ReceivedMessage|EXCEPTION| {ex.Message}");
            }
            return lRes;
        }

        private async Task<List<HandleMessageModel>> HandleMessage(Message msg)
        {
            var lRes = new List<HandleMessageModel>();
            try
            {
                var input = msg.Text.RemoveSpace().ToUpper();
                if ((input.StartsWith("[") && input.EndsWith("]"))
                    || (input.StartsWith("*") && input.EndsWith("*"))
                    || (input.StartsWith("@") && input.EndsWith("@")))
                {

                }
                else if (input.Length == 3) //Mã chứng khoán
                {
                    var overview = await Overview(input.ToUpper(), false);
                    if (overview != null) 
                    { 
                        lRes.Add(overview);
                    }
                    var lMa = await ChartChungKhoan(input);
                    if (lMa?.Any() ?? false)
                    {
                        lRes.AddRange(lMa);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"MessageService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return lRes;
        }

        private async Task<HandleMessageModel> Overview(string code, bool isNganh)
        {
            try
            {
                var curDirectory = Directory.GetCurrentDirectory();
                var path = string.Empty;
                if (isNganh)
                {
                    path = $"{curDirectory}/Resource/Nganh/{code}.png";
                }
                else
                {
                    path = $"{curDirectory}/Resource/{code}.png";
                }
                if (File.Exists(path))
                {
                    var file = new FileStream(path, FileMode.Open);
                    return new HandleMessageModel
                    {
                        Stream = InputFile.FromStream(file)
                    };
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"MessageService.Overview|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<List<HandleMessageModel>> ChartChungKhoan(string input)
        {
            var lRes = new List<HandleMessageModel>();
            try
            {
                var lStream = await _chartService.Chart_MaCK(input);
                if (lStream is null)
                    return null;
                foreach (var stream in lStream)
                {
                    if (stream is null) 
                        continue;
                    lRes.Add(new HandleMessageModel
                    {
                        Stream = stream
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TeleService.MaChungKhoan|EXCEPTION|{ex.Message}");
            }
            return lRes;
        }
    }
}