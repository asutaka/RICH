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
        private readonly IScraperService _scraperService;
        private readonly IConfigNewsRepo _configRepo;
        public NewsService(ILogger<NewsService> logger, IScraperService scraperService, IConfigNewsRepo configRepo)
        {
            _logger = logger;
            _scraperService = scraperService;
            _configRepo = configRepo;
        }

        public async Task<List<string>> GetNews()
        {
            var lNews = new List<string>();
            try
            {
                var now = DateTime.Now;
                var time = int.Parse($"{now.Year}{now.Month.To2Digit()}{now.Day.To2Digit()}");

                var KinhTeChungKhoan = await _scraperService.News_KinhTeChungKhoan();
                if(KinhTeChungKhoan != null)
                {
                    foreach (var item in KinhTeChungKhoan.data.articles)
                    {
                        var builder = Builders<ConfigNews>.Filter;
                        var news = _configRepo.GetEntityByFilter(builder.And(
                            builder.Eq(x => x.key, item.PublisherId.ToString())
                        ));

                        if (news != null)
                            continue;

                        _configRepo.InsertOne(new ConfigNews
                        {
                            d = time,
                            key = item.PublisherId.ToString()
                        });

                        lNews.Add($"[|Kinh Tế Chứng Khoán|{item.Title}]({item.LinktoMe2})");
                    }
                }

                var lNguoiDuaTin = await _scraperService.News_NguoiDuaTin();
                if (lNguoiDuaTin.Any())
                {
                    foreach (var item in lNguoiDuaTin)
                    {
                        var builder = Builders<ConfigNews>.Filter;
                        var news = _configRepo.GetEntityByFilter(builder.And(
                            builder.Eq(x => x.key, item.ID)
                        ));

                        if (news != null)
                            continue;

                        _configRepo.InsertOne(new ConfigNews
                        {
                            d = time,
                            key = item.ID
                        });

                        lNews.Add($"[|Người Đưa Tin|{item.Title}]({item.LinktoMe2})");
                    }
                }
                //var lNguoiQuanSat = await _scraperService.News_NguoiQuanSat();
            }
            catch (Exception ex)
            {
                _logger.LogError($"F319Service.F319KOL|EXCEPTION| {ex.Message}");
            }
            return lNews;
        }
    }
}
