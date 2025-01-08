using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigBaoCaoPhanTichRepo : IBaseRepo<ConfigBaoCaoPhanTich>
    {
    }

    public class ConfigBaoCaoPhanTichRepo : BaseRepo<ConfigBaoCaoPhanTich>, IConfigBaoCaoPhanTichRepo
    {
        public ConfigBaoCaoPhanTichRepo()
        {
        }
    }
}
