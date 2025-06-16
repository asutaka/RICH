using StockPr.DAL;
using StockPr.Model.BCPT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPr.Service
{
    public interface INewsService
    {
        Task GetNews();
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

        public async Task GetNews()
        {
            //var lOutput = new List<F319Model>();
            try
            {
                var lNguoiQuanSat = await _apiService.News_NguoiQuanSat();
            }
            catch (Exception ex)
            {
                _logger.LogError($"F319Service.F319KOL|EXCEPTION| {ex.Message}");
            }
            //return lOutput;
        }
    }
}
