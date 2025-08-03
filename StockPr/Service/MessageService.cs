using StockPr.DAL;
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
        private readonly IEPSRankService _rankService;

        private readonly int _ty = (int)EUserMessageType.StockPr;
        public MessageService(ILogger<MessageService> logger, IChartService chartService, IUserMessageRepo userMessageRepo, IEPSRankService rankService)
        {
            _logger = logger;
            _chartService = chartService;
            _userMessageRepo = userMessageRepo;
            _rankService = rankService;
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
            var now = DateTime.Now;
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
                    var lCungCau = await ChartCungCau(input, now.AddDays(-28), now);
                    if (lCungCau?.Any() ?? false)
                    {
                        lRes.AddRange(lCungCau);
                    }
                    var rank = await _rankService.FreeFloat(input.ToUpper());
                    if (rank.Item1 > 0)
                    {
                        lRes.Add(new HandleMessageModel
                        {
                            Message = $"FreeFloat: {Math.Round(rank.Item1)}%\nEPS: {Math.Round(rank.Item2).ToString("#,##0.#")} đồng\nPE: {Math.Round(rank.Item3)}\nPB: {Math.Round(rank.Item4)}\nNợ/VCSH: {Math.Round(rank.Item5, 2)}"
                        });
                    }
                }
                else
                {
                    var mes = msg.Text.Trim();
                    var lmes = mes.Split(" ");
                    if(lmes.Length > 1
                        && lmes[0].Equals("NN", StringComparison.OrdinalIgnoreCase))
                    {
                        var maStr = lmes[1].ToUpper();
                        if(maStr.Length == 3)//MaCK
                        {
                            var count = lmes.Length;
                            if (count == 3) //Kèm ngày
                            {
                                var dateStr = lmes[2].Replace("-", "/").Replace(".", "/").Replace(",", "/");
                                var lDate = dateStr.Split("/");
                                if(lDate.Length == 2)//Date chỉ cần truyền ngày và tháng
                                {
                                    var isInt = int.TryParse(lDate[0], out var day);
                                    if (isInt)
                                    {
                                        isInt = int.TryParse(lDate[1], out var month);
                                        if (isInt)
                                        {
                                            //Chart + Ngày
                                            var year = now.Year;
                                            if (month > now.Month)
                                                year--;

                                            var from = new DateTime(year, month, day);
                                            var to = from.AddDays(28);
                                            var lCungCauDate = await ChartCungCau(maStr, from, to);
                                            if (lCungCauDate?.Any() ?? false)
                                            {
                                                lRes.AddRange(lCungCauDate);
                                            }
                                            return lRes;
                                        }
                                    }
                                }
                            }
                            //Chart
                            var lCungCau = await ChartCungCau(maStr, now.AddDays(-28), now);
                            if (lCungCau?.Any() ?? false)
                            {
                                lRes.AddRange(lCungCau);
                            }
                            return lRes;
                        }
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
                _logger.LogError($"TeleService.ChartChungKhoan|EXCEPTION|{ex.Message}");
            }
            return lRes;
        }

        private async Task<List<HandleMessageModel>> ChartCungCau(string input, DateTime from, DateTime to)
        {
            var lRes = new List<HandleMessageModel>();
            try
            {
                var lStream = await _chartService.Chart_CungCau(input, from, to);
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
                _logger.LogError($"TeleService.ChartCungCau|EXCEPTION|{ex.Message}");
            }
            return lRes;
        }
    }
}
