using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradePr.DAL;

namespace TradePr.Service
{
    public interface ISyncDataService
    {

    }
    public class SyncDataService : ISyncDataService
    {
        private readonly ILogger<SyncDataService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly ISymbolConfigRepo _symConfigRepo;
        public SyncDataService(ILogger<SyncDataService> logger,
                           IAPIService apiService, ITeleService teleService, ISymbolConfigRepo symConfigRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symConfigRepo = symConfigRepo;
        }

        public async Task DetectLong()
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }
    }
}
