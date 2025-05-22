using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using TradePr.Utils;
using TradePr.Model;
using TradePr.DAL;
using TradePr.DAL.Entity;
using MongoDB.Driver;

namespace TradePr.Service
{
    public interface ITeleService
    {
        Task SendMessage(long id, string mes);

    }
    public class TeleService : ITeleService
    {
        private const long _idUser = 1066022551;
        private readonly ILogger<TeleService> _logger;
        private TelegramBotClient _bot = new TelegramBotClient("7783423206:AAGnpRM_8xnxr44sbgOX-ktIHXQEsyMxH6A");
        private readonly ISymbolRepo _symRepo;
        private readonly IConfigDataRepo _configRepo;
        public TeleService(ILogger<TeleService> logger, ISymbolRepo symRepo, IConfigDataRepo configRepo)
        {
            _logger = logger;
            _symRepo = symRepo;
            _bot.OnMessage += OnMessage;
            _configRepo = configRepo;
        }
        async Task OnMessage(Message msg, UpdateType type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msg.Text)
                    || type != UpdateType.Message) 
                    return;
                Console.WriteLine(msg.Text);
                var lMes = msg.Text.Trim().Replace("  "," ").Split(' ');
                var lExchange = new List<EKey>
                {
                    EKey.Binance,
                    EKey.Bybit
                };
                var lTerminal = new List<EKey>
                {
                    EKey.Start,
                    EKey.Stop
                };
                var lPos = new List<EKey>
                {
                    EKey.Long,
                    EKey.Short
                };
                var lAction = new List<EKey> 
                { 
                    EKey.Add, 
                    EKey.Delete 
                };
                var lThread = new List<EKey>
                {
                    EKey.Thread1,
                    EKey.Thread2,
                    EKey.Thread3,
                    EKey.Thread4,
                    EKey.Thread5
                };
                var lMax = new List<EKey>
                {
                    EKey.Max10,
                    EKey.Max20,
                    EKey.Max30,
                    EKey.Max40,
                    EKey.Max50,
                    EKey.Max60,
                    EKey.Max70,
                    EKey.Max80,
                    EKey.Max100,
                    EKey.Max120,
                    EKey.Max150,
                    EKey.Max200,
                    EKey.Max250,
                    EKey.Max300,
                    EKey.Max400,
                    EKey.Max500,
                    EKey.Max700,
                    EKey.Max800,
                    EKey.Max1000,
                };

                var model = new clsTerminal();
                var first = lMes[0];
                if(lExchange.Any(x => first.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    var ex = lExchange.First(x => first.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase));
                   
                    model.Exchange = ex;
                    if (lMes.Length > 1)
                    {
                        var mes = lMes[1].Replace("-", "").Replace("_", "");
                        BuildModel(mes);
                    }
                    if (lMes.Length > 2)
                    {
                        var mes = lMes[2].Replace("-", "").Replace("_", "");
                        BuildModel(mes);
                    }
                    if (lMes.Length > 3)
                    {
                        var mes = lMes[3].Replace("-", "").Replace("_", "");
                        BuildModel(mes);
                    }
                    if (lMes.Length > 4)
                    {
                        var mes = lMes[4].Replace("-", "").Replace("_", "");
                        BuildModel(mes);
                    }
                }

                void BuildModel(string mes)
                {
                    var terminal = lTerminal.FirstOrDefault(x => mes.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase));
                    var pos = lPos.FirstOrDefault(x => mes.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase));
                    var thread = lThread.FirstOrDefault(x => mes.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase));
                    var max = lMax.FirstOrDefault(x => mes.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase));
                    var action = lAction.FirstOrDefault(x => mes.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase));
                    if(model.Terminal is null 
                        && terminal > 0)
                    {
                        model.Terminal = terminal;
                    }
                    if(model.Position is null
                        && pos > 0)
                    {
                        model.Position = pos;
                    }
                    if(model.Action is null
                        && action > 0)
                    {
                        model.Action = action;
                    }
                    if(model.Max is null
                        && max > 0)
                    {
                        model.Max = max;
                    }
                    if(model.Thread is null
                        && thread > 0)
                    {
                        model.Thread = thread;
                    }
                    if (mes.ToUpper().Contains("USDT")
                        && string.IsNullOrWhiteSpace(model.Coin))
                    {
                        model.Coin = mes.ToUpper();
                    }
                    if(mes.Equals("bl", StringComparison.OrdinalIgnoreCase)
                        || mes.Equals("balance", StringComparison.OrdinalIgnoreCase))
                    {
                        model.Balance = true;
                    }
                }

                if(model.Exchange != null)
                {
                    //Thực thi lệnh
                    if (!string.IsNullOrWhiteSpace(model.Coin))
                    {
                        if(model.Action != null && model.Position != null)
                        {
                            //Thêm bớt coin
                            ThemBotCoin(model);
                            await SendMessage(_idUser, $"[{model.Exchange.ToString().ToUpper()}] Đã {model.Action.ToString().ToUpper()} {model.Coin} đối với danh sách {model.Position.ToString().ToUpper()}");
                        }
                    }
                    else if(model.Max != null)
                    {
                        //Set lại giá trị lệnh
                        SetGiaTriLenh(model);
                        await SendMessage(_idUser, $"[{model.Exchange.ToString().ToUpper()}] Đã set giá trị Lệnh là {model.Max.ToString().ToUpper().Replace("MAX", "")}");
                    }
                    else if(model.Thread != null)
                    {
                        //Set số lệnh trong cùng một thời điểm
                        SetThread(model);
                        await SendMessage(_idUser, $"[{model.Exchange.ToString().ToUpper()}] Đã set số Thread tối đa là {model.Thread.ToString().ToUpper().Replace("THREAD", "")}");
                    }
                    else if(model.Terminal != null)
                    {
                        if(model.Position != null)
                        {
                            //Bật tắt một vị thế
                            BatTatMotViThe(model);
                            await SendMessage(_idUser, $"[{model.Exchange.ToString().ToUpper()}] Đã {model.Terminal.ToString().ToUpper()} các vị thế {model.Position.ToString().ToUpper()}");
                        }
                        else
                        {
                            //Bật tắt toàn bộ 
                            BatTatToanBo(model);
                            await SendMessage(_idUser, $"[{model.Exchange.ToString().ToUpper()}] Đã {model.Terminal.ToString().ToUpper()} toàn bộ các vị thế");
                        }
                    }
                    else if(model.Balance)
                    {
                        if(model.Exchange == EKey.Bybit)
                        {
                            var lIncome = await StaticVal.ByBitInstance().V5Api.Account.GetTransactionHistoryAsync(limit: 200);
                            if (lIncome.Success && lIncome.Data.List.Any())
                            {
                                var balance = lIncome.Data.List.First();
                                await SendMessage(_idUser, $"Balance: {Math.Round(balance.CashBalance.Value, 1)}$");
                            }
                        }
                    }
                }

                Console.WriteLine(msg.Text);

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

        private void ThemBotCoin(clsTerminal model)
        {
            var ex = model.Exchange == EKey.Binance ? (int)EExchange.Binance : (int)EExchange.Bybit;
            var pos = model.Position == EKey.Long ? (int)Binance.Net.Enums.OrderSide.Buy : (int)Binance.Net.Enums.OrderSide.Sell;
            var builder = Builders<Symbol>.Filter;
            var entity = _symRepo.GetEntityByFilter(builder.And(
                builder.Eq(x => x.ex, ex),
                builder.Eq(x => x.ty, pos),
                builder.Eq(x => x.s, model.Coin)
            ));

            if (model.Action == EKey.Add)
            {
                if(entity is null)
                {
                    _symRepo.InsertOne(new Symbol
                    {
                        s = model.Coin,
                        ex = ex,
                        ty = pos,
                        rank = 1
                    });
                }
            }
            else
            {
                if(entity != null)
                {
                    _symRepo.DeleteOne(builder.And(
                        builder.Eq(x => x.ex, ex),
                        builder.Eq(x => x.ty, pos),
                        builder.Eq(x => x.s, model.Coin)
                    ));
                }
            }
        }

        private void SetGiaTriLenh(clsTerminal model)
        {
            var lConfig = _configRepo.GetAll();
            var ex = model.Exchange == EKey.Binance ? (int)EExchange.Binance : (int)EExchange.Bybit;
            var max = int.Parse(model.Max.ToString().ToUpper().Replace("MAX", ""));

            var config = lConfig.FirstOrDefault(x => x.ex == ex && x.op == (int)EOption.Max);
            if (config != null)
            {
                config.value = max;
                _configRepo.Update(config);
            }
            else
            {
                _configRepo.InsertOne(new ConfigData
                {
                    ex = ex,
                    op = (int)EOption.Max,
                    value = max,
                    status = 1
                });
            }
        }

        private void SetThread(clsTerminal model)
        {
            var lConfig = _configRepo.GetAll();
            var ex = model.Exchange == EKey.Binance ? (int)EExchange.Binance : (int)EExchange.Bybit;
            var thread = int.Parse(model.Thread.ToString().ToUpper().Replace("THREAD", ""));

            var config = lConfig.FirstOrDefault(x => x.ex == ex && x.op == (int)EOption.Thread);
            if (config != null)
            {
                config.value = thread;
                _configRepo.Update(config);
            }
            else
            {
                _configRepo.InsertOne(new ConfigData
                {
                    ex = ex,
                    op = (int)EOption.Thread,
                    value = thread,
                    status = 1
                });
            }
        }

        private void BatTatToanBo(clsTerminal model)
        {
            var lConfig = _configRepo.GetAll();
            var ex = model.Exchange == EKey.Binance ? (int)EExchange.Binance : (int)EExchange.Bybit;
            var terminal = model.Terminal == EKey.Start ? 0 : 1;

            var config = lConfig.FirstOrDefault(x => x.ex == ex && x.op == (int)EOption.DisableAll);
            if(config != null)
            {
                config.status = terminal;
                _configRepo.Update(config);
            }
            else
            {
                _configRepo.InsertOne(new ConfigData
                {
                    ex = ex,
                    op = (int)EOption.DisableAll,
                    status = terminal
                });
            }
        }

        private void BatTatMotViThe(clsTerminal model)
        {
            var lConfig = _configRepo.GetAll();
            var ex = model.Exchange == EKey.Binance ? (int)EExchange.Binance : (int)EExchange.Bybit;
            var pos = model.Position == EKey.Long ? (int)EOption.DisableLong : (int)EOption.DisableShort;
            var terminal = model.Terminal == EKey.Start ? 0 : 1;

            var config = lConfig.FirstOrDefault(x => x.ex == ex && x.op == pos);
            if (config != null)
            {
                config.status = terminal;
                _configRepo.Update(config);
            }
            else
            {
                _configRepo.InsertOne(new ConfigData
                {
                    ex = ex,
                    op = pos,
                    status = terminal
                });
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
