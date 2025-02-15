
using TestPr.DAL.Entity;

namespace TestPr.DAL
{
    public interface ITokenUnlockRepo : IBaseRepo<TokenUnlock>
    {
    }

    public class TokenUnlockRepo : BaseRepo<TokenUnlock>, ITokenUnlockRepo
    {
        public TokenUnlockRepo()
        {
        }
    }
}
