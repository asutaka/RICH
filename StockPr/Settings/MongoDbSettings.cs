namespace StockPr.Settings
{
    /// <summary>
    /// MongoDB connection configuration
    /// </summary>
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
