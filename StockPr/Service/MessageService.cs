using Microsoft.Extensions.Logging.EventLog;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using Telegram.Bot.Types;
using System.Collections.Concurrent;

namespace StockPr.Service
{
    public interface IMessageService
    {
        Task<List<HandleMessageModel>> ReceivedMessage(Message msg);
    }
    public class MessageService : IMessageService
    {
        private readonly ILogger<MessageService> _logger;
        
        // ⚡ Thread-safe collections for concurrent user handling
        private static readonly ConcurrentDictionary<long, UserMessage> _userMessages 
            = new ConcurrentDictionary<long, UserMessage>();
        private static readonly ConcurrentDictionary<long, SemaphoreSlim> _userSemaphores 
            = new ConcurrentDictionary<long, SemaphoreSlim>();
        
        private readonly IUserMessageRepo _userMessageRepo;
        private readonly IAccountRepo _accountRepo;
        private readonly IChartService _chartService;
        private readonly IEPSRankService _rankService;
        private readonly IAIService _aiService;

        private readonly int _ty = (int)EUserMessageType.StockPr;
        public MessageService(ILogger<MessageService> logger, IChartService chartService, IUserMessageRepo userMessageRepo, IEPSRankService rankService, IAccountRepo accountRepo, IAIService ai)
        {
            _logger = logger;
            _chartService = chartService;
            _userMessageRepo = userMessageRepo;
            _rankService = rankService;
            _accountRepo = accountRepo;
            _aiService = ai;
        }
        public async Task<List<HandleMessageModel>> ReceivedMessage(Message msg)
        {
            // ⚡ Per-user locking: Nhiều users chạy song song, cùng user chạy tuần tự
            var semaphore = _userSemaphores.GetOrAdd(msg.Chat.Id, 
                _ => new SemaphoreSlim(1, 1));
            
            await semaphore.WaitAsync();
            try
            {
                return await ProcessMessage(msg);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<List<HandleMessageModel>> ProcessMessage(Message msg)
        {
            var lRes = new List<HandleMessageModel>();
            try
            {
                #region Lưu trữ thời gian mới nhất của từng User (Thread-safe)
                var time = new DateTimeOffset(msg.Date, TimeSpan.Zero).ToUnixTimeSeconds();
                
                var userMessage = _userMessages.AddOrUpdate(
                    msg.Chat.Id,
                    // Add new user
                    _ => 
                    {
                        var newMsg = new UserMessage
                        {
                            u = msg.Chat.Id,
                            ty = _ty,
                            t = time
                        };
                        _userMessageRepo.InsertOne(newMsg);
                        return newMsg;
                    },
                    // Update existing user
                    (_, existing) =>
                    {
                        if (existing.t >= time)
                            return existing; // Skip old message
                        
                        existing.t = time;
                        _userMessageRepo.Update(existing);
                        return existing;
                    }
                );

                // Check if message is too old
                if (userMessage.t > time)
                    return null;
                #endregion

                var lAccount = _accountRepo.GetAll();
                var exists = lAccount.FirstOrDefault(x => x.u == msg.Chat.Id);
                if (exists is null)
                {
                    _accountRepo.InsertOne(new Account
                    {
                        u = msg.Chat.Id,
                        name = $"{msg.Chat.FirstName} {msg.Chat.LastName}",
                        status = 0
                    });

                    return null;
                }

                if (exists.status <= 0)
                    return null;

                lRes.AddRange(await HandleMessage(msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MessageService.ProcessMessage|EXCEPTION| {ex.Message}");
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
                    // ⚡ PARALLEL EXECUTION: Chạy 3 tasks cùng lúc
                    var overviewTask = Overview(input.ToUpper(), false);
                    var chartsTask = ChartChungKhoan(input);
                    var rankTask = _rankService.FreeFloat(input.ToUpper());
                    
                    await Task.WhenAll(overviewTask, chartsTask, rankTask);
                    
                    // Collect results
                    var overview = await overviewTask;
                    if (overview != null)
                    {
                        lRes.Add(overview);
                    }
                    
                    var lMa = await chartsTask;
                    if (lMa?.Any() ?? false)
                    {
                        lRes.AddRange(lMa);
                    }
                    
                    var rank = await rankTask;
                    if (rank.Item1 > 0)
                    {
                        lRes.Add(new HandleMessageModel
                        {
                            Message = $"FreeFloat: {Math.Round(rank.Item1)}%\nEPS: {Math.Round(rank.Item2).ToString("#,##0.#")} đồng\nPE: {Math.Round(rank.Item3, 1)}\nPB: {Math.Round(rank.Item4, 2)}\nNợ/VCSH: {Math.Round(rank.Item5, 2)}"
                        });
                    }

                    //var ai = await _aiService.AskModel(input.ToUpper());
                    //if(ai.Item1)
                    //{
                    //    lRes.Add(new HandleMessageModel
                    //    {
                    //        Message = ai.Item2
                    //    });
                    //}    
                }
                else
                {
                    var mes = msg.Text.Trim();
                    var lmes = mes.Split(" ");
                    if (lmes.Length <= 0)
                        return lRes;

                    if(lmes[0].Equals("NN", StringComparison.OrdinalIgnoreCase))
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
                            //chart Thành phần
                            var stream = await _chartService.Chart_ThongKeKhopLenh(maStr);
                            if(stream != null)
                            {
                                lRes.Add(new HandleMessageModel
                                {
                                    Stream = stream,
                                });
                            }
                            return lRes;
                        }
                    }
                    else if (lmes[0].Equals("long", StringComparison.OrdinalIgnoreCase))
                    {
                        var maStr = lmes[1].ToUpper();
                        if (maStr.Length == 3
                            || maStr.Equals("VNINDEX"))//MaCK
                        {
                            //chart Thành phần
                            var stream = await _chartService.Chart_ThongKeKhopLenh_Long(maStr);
                            if (stream != null)
                            {
                                lRes.Add(new HandleMessageModel
                                {
                                    Stream = stream,
                                });
                            }
                            return lRes;
                        }
                    }
                    else if (lmes[0].Equals("RANK", StringComparison.OrdinalIgnoreCase))
                    {
                        var maStr = lmes[1].ToUpper();
                        var dic = new Dictionary<string, Money24h_StatisticResponse>();
                        if (maStr.Equals("bank", StringComparison.OrdinalIgnoreCase)
                            || maStr.Equals("nh", StringComparison.OrdinalIgnoreCase)
                            || maStr.Equals("nganhang", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lBankTP);
                        }
                        else if (maStr.Equals("ck", StringComparison.OrdinalIgnoreCase)
                                || maStr.Equals("chungkhoan", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lChungKhoanTP);
                        }
                        else if (maStr.Equals("thep", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lThepTP);
                        }
                        else if (maStr.Equals("kcn", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lKCNTP);
                        }
                        else if (maStr.Equals("dtc", StringComparison.OrdinalIgnoreCase)
                                || maStr.Equals("dautucong", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lDTC_TP);
                        }
                        else if (maStr.Equals("bds", StringComparison.OrdinalIgnoreCase)
                              || maStr.Equals("batdongsan", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lBDS_TP);
                        }
                        else if (maStr.Equals("thuysan", StringComparison.OrdinalIgnoreCase) 
                                || maStr.Equals("ts", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lThuysanTP);
                        }
                        else if (maStr.Equals("daukhi", StringComparison.OrdinalIgnoreCase) 
                                || maStr.Equals("dk", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lDaukhiTP);
                        }
                        else if (maStr.Equals("detmay", StringComparison.OrdinalIgnoreCase)
                                || maStr.Equals("dm", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lDetmayTP);
                        }
                        else if (maStr.Equals("phanbon", StringComparison.OrdinalIgnoreCase)
                                || maStr.Equals("pb", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lPhanbonTP);
                        }
                        else if (maStr.Equals("banle", StringComparison.OrdinalIgnoreCase)
                                || maStr.Equals("bl", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lBanleTP);
                        }
                        else if (maStr.Equals("dien", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lDienTP);
                        }
                        else if (maStr.Equals("khoangsan", StringComparison.OrdinalIgnoreCase)
                                || maStr.Equals("ks", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lKhoangsanTP);
                        }
                        else if (maStr.Equals("khac", StringComparison.OrdinalIgnoreCase))
                        {
                            dic = await _rankService.RankMaCK(StaticVal._lKhacTP);
                        }

                        // ⚡ PARALLEL CHART GENERATION: Tạo tất cả charts cùng lúc
                        var chartTasks = dic.Select(async item =>
                        {
                            try
                            {
                                var stream = await _chartService.Chart_ThongKeKhopLenh(item.Key, item.Value);
                                return stream != null ? new HandleMessageModel { Stream = stream } : null;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to generate chart for {item.Key}");
                                return null;
                            }
                        });
                        
                        var charts = await Task.WhenAll(chartTasks);
                        lRes.AddRange(charts.Where(c => c != null));

                        return lRes;
                    }
                    else if(lmes[0].Equals("focus", StringComparison.OrdinalIgnoreCase))
                    {
                        var lFocus = await _rankService.ListFocus();
                        var message = lFocus.Any() ? $"{string.Join(",", lFocus)}" : "Không có dữ liệu";
                        lRes.Add(new HandleMessageModel
                        {
                            Message = message
                        });
                    }
                    else if (lmes[0].Equals("list", StringComparison.OrdinalIgnoreCase))
                    {
                        var lAccount = _accountRepo.GetAll();
                        var exists = lAccount.FirstOrDefault(x => x.u == msg.Chat.Id);
                        if (exists != null)
                        {
                            if (lmes.Length == 1)
                            {
                                if(exists.list?.Any() ?? false)
                                {
                                    var message = string.Join(", ", exists.list);
                                    lRes.Add(new HandleMessageModel
                                    {
                                        Message = $"Danh sách theo dõi: {message}"
                                    });
                                }
                                else
                                {
                                    lRes.Add(new HandleMessageModel
                                    {
                                        Message = "Chưa tạo danh sách theo dõi"
                                    });
                                }
                            }
                            else if(lmes.Length == 2)
                            {
                                if(lmes[1].Equals("delete", StringComparison.OrdinalIgnoreCase)
                                    || lmes[1].Equals("xoa", StringComparison.OrdinalIgnoreCase)
                                    || lmes[1].Equals("clear", StringComparison.OrdinalIgnoreCase))
                                {
                                    exists.list = new List<string>();
                                    _accountRepo.Update(exists);
                                }
                                else
                                {
                                    var lMaCK = lmes[1].ToUpper().Split(",");
                                    if(lMaCK.Length > 10)
                                    {
                                        lRes.Add(new HandleMessageModel
                                        {
                                            Message = "Danh sách theo dõi quá dài"
                                        });
                                    }
                                    else
                                    {
                                        var lList = new List<string>();
                                        foreach (var item in lMaCK)
                                        {
                                            var check = StaticVal._lStock.FirstOrDefault(x => x.s == item.Trim() && x.ex == (int)EExchange.HSX);
                                            if(check != null)
                                            {
                                                lList.Add(item.Trim());
                                            }
                                        }
                                        exists.list = lList;
                                        _accountRepo.Update(exists);
                                    }
                                }
                            }
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
