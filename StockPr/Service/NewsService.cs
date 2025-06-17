using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Model.BCPT;
using StockPr.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPr.Service
{
    public interface INewsService
    {
        Task<List<string>> GetNews();
    }
    public class NewsService : INewsService
    {
        private readonly ILogger<NewsService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigNewsRepo _configRepo;
        public NewsService(ILogger<NewsService> logger, IAPIService apiService, IConfigNewsRepo configRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
        }

        public async Task<List<string>> GetNews()
        {
            var lNews = new List<string>();
            try
            {
                var now = DateTime.Now;
                var time = int.Parse($"{now.Year}{now.Month.To2Digit()}{now.Day.To2Digit()}");
                

                var KinhTeChungKhoan = await _apiService.News_KinhTeChungKhoan();
                if(KinhTeChungKhoan != null)
                {
                    foreach (var item in KinhTeChungKhoan.data.articles)
                    {
                        var builder = Builders<ConfigNews>.Filter;
                        var news = _configRepo.GetEntityByFilter(builder.And(
                            builder.Eq(x => x.d, time),
                            builder.Eq(x => x.key, item.PublisherId.ToString())
                        ));

                        if (news != null)
                            continue;

                        _configRepo.InsertOne(new ConfigNews
                        {
                            d = time,
                            key = item.PublisherId.ToString()
                        });

                        lNews.Add($"[{item.Title}]({item.LinktoMe2})");
                    }
                }


                //var lNguoiQuanSat = await _apiService.News_NguoiQuanSat();
            }
            catch (Exception ex)
            {
                _logger.LogError($"F319Service.F319KOL|EXCEPTION| {ex.Message}");
            }
            return lNews;
        }
    }
}
