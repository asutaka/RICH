namespace StockExtendPr.DAL
{
    public interface IConfigDataRepo : IBaseRepo<ConfigData>
    {
    }

    public class ConfigDataRepo : BaseRepo<ConfigData>, IConfigDataRepo
    {
        public ConfigDataRepo()
        {
        }
    }
}
