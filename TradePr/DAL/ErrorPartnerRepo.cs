using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface IErrorPartnerRepo : IBaseRepo<ErrorPartner>
    {
    }

    public class ErrorPartnerRepo : BaseRepo<ErrorPartner>, IErrorPartnerRepo
    {
        public ErrorPartnerRepo()
        {
        }
    }
}
