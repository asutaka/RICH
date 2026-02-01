using Microsoft.Playwright;

namespace StockPr.Service
{
    public interface IVietstockSessionManager
    {
        AuthSession? Session { get; set; }
    }

    public class VietstockSessionManager : IVietstockSessionManager
    {
        public AuthSession? Session { get; set; }
    }
}
