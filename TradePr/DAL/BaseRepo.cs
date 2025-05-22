using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using TradePr.DAL.Entity;
using TradePr.DAL.Settings;

namespace TradePr.DAL
{
    public interface IBaseRepo<T> where T : BaseDTO
    {
        /// <summary>
        /// Sets a collection
        /// </summary>
        bool SetCollection(string collectionName);


        /// <summary>
        /// Get all entities in collection
        /// </summary>
        /// <returns>collection of entities</returns>
        List<T> GetAll();
        /*
            FilterDefinition<ThongKe> filter = null;
            var builder = Builders<ThongKe>.Filter;
            var lFilter = new List<FilterDefinition<ThongKe>>()
            {
                builder.Eq(x => x.date, content.date),
                builder.Eq(x => x.symbol, content.symbol),
            };
            foreach (var itemFilter in lFilter)
            {
                if (filter is null)
                {
                    filter = itemFilter;
                    continue;
                }
                filter &= itemFilter;
            }
         */

        //var builder = Builders<SignalTrade>.Filter;
        //var lSignal = _signalTradeRepo.GetByFilter(builder.And(
        //    builder.Eq(x => x.ex, _exchange),
        //    builder.Eq(x => x.timeFlag, time)
        //));


        //Example: FilterDefinition<StockPr> filter = Builders<StockPr>.Filter.Eq(x => x.s, itemMa.symbol);
        List<T> GetByFilter(FilterDefinition<T> filter, int offset = 0, int limit = 0);
        T GetEntityByFilter(FilterDefinition<T> filter);

        /// <summary>
        /// Get async entity by identifier 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <returns>Entity</returns>
        T GetById(string id);

        IQueryable<T> Table { get; }


        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        T Update(T entity);

        /// <summary>
        /// Async Update one field entity
        /// </summary>
        /// <param name="entity">Entity</param>
        bool UpdateOneField(string fieldName, dynamic value, FilterDefinition<T> filter);
        void InsertOne(T entity);
        void DeleteOne(FilterDefinition<T> filter);
        void DeleteMany(FilterDefinition<T> filter);

    }

    public abstract class BaseRepo<T> : IBaseRepo<T> where T : BaseDTO
    {
        #region Fields
        protected IMongoCollection<T> _collection;

        protected IMongoDatabase _database;
        protected ILogger<BaseRepo<T>> _logger;

        public IMongoCollection<T> Collection => _collection;

        /// <summary>
        /// Gets a table
        /// </summary>
        public virtual IQueryable<T> Table => _collection.AsQueryable(new AggregateOptions { AllowDiskUse = true });

        /// <summary>
        /// Sets a collection
        /// </summary>
        public virtual bool SetCollection(string collectionName)
        {
            _collection = _collection.Database.GetCollection<T>(collectionName);
            return true;
        }

        public virtual string GetCollectionName()
        {
            return typeof(T).Name;
            //return "answer";
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>        
        public BaseRepo()
        {
            //var client = new MongoClient(MongoDataSettingsManager.ConnectionString);

            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(MongoSetting.ConnectionString));
            clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
            var client = new MongoClient(clientSettings);


            _database = client.GetDatabase(MongoSetting.Database);
            _collection = _database.GetCollection<T>(GetCollectionName());
        }

        public BaseRepo(IMongoDatabase database, ILogger<BaseRepo<T>> logger)
        {
            _database = database;
            _collection = _database.GetCollection<T>(GetCollectionName());
            _logger = logger;
        }

        /// <summary>
        /// Get all entities in collection
        /// </summary>
        /// <returns>collection of entities</returns>
        public virtual List<T> GetAll()
        {
            try
            {
                var enties = _collection.AsQueryable().ToList();

                return enties;
            }
            catch (Exception ex)
            {
                _logger.LogError($"IMongoRepositoryBase.GetAllAsync|REPOSITORY: {typeof(T).Name.ToUpper()}Repository|EXCEPTION| {ex.Message}");
            }
            return null;

        }

        /// <summary>
        /// Get async entity by identifier 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <returns>Entity</returns>
        public virtual T GetById(string id)
        {
            return _collection.Find(e => e.ObjectId == id).FirstOrDefault();
        }

        public virtual T Update(T entity)
        {
            try
            {
                _collection.ReplaceOne(x => x.ObjectId == entity.ObjectId, entity, new ReplaceOptions() { IsUpsert = false });
            }
            catch (Exception ex)
            {
                _logger.LogError($"IMongoRepositoryBase.Update|REPOSITORY: {typeof(T).Name.ToUpper()}Repository|EXCEPTION| {ex.Message}");
            }
            return entity;
        }

        public bool UpdateOneField(string fieldName, dynamic value, FilterDefinition<T> filter)
        {
            var result = false;
            try
            {
                var update = Builders<T>.Update.Set(fieldName, value);

                UpdateResult updateResult = _collection.UpdateOne(filter, update);
                result = updateResult.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError($"IMongoRepositoryBase.UpdateOneFieldAsync|REPOSITORY: {typeof(T).Name.ToUpper()}Repository|EXCEPTION| {ex.Message}");
                result = false;
            }

            return result;
        }


        public void InsertOne(T entity)
        {
            try
            {
                _collection.InsertOne(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError($"IMongoRepositoryBase.InsertOneAsync|REPOSITORY: {typeof(T).Name.ToUpper()}Repository|EXCEPTION| {ex.Message}");
            }
        }

        public void DeleteOne(FilterDefinition<T> filter)
        {
            try
            {
                _collection.DeleteOne(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError($"IMongoRepositoryBase.DeleteOneAsync|REPOSITORY: {typeof(T).Name.ToUpper()}Repository|EXCEPTION| {ex.Message}");
            }
        }

        public void DeleteMany(FilterDefinition<T> filter)
        {
            try
            {
                _collection.DeleteMany(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError($"IMongoRepositoryBase.DeleteMany|REPOSITORY: {typeof(T).Name.ToUpper()}Repository|EXCEPTION| {ex.Message}");
            }
        }
        //Example: FilterDefinition<StockPr> filter = Builders<StockPr>.Filter.Eq(x => x.s, itemMa.symbol);
        public List<T> GetByFilter(FilterDefinition<T> filter, int offset = 0, int limit = 0)
        {
            if (filter is null)
            {

                if (offset > 0)
                {
                    return _collection.AsQueryable().Skip((offset - 1) * limit)
                    .Take(limit).ToList();
                }

                return _collection.AsQueryable().ToList();
            }
            else
            {
                if (offset > 0)
                {
                    return _collection.Find(filter).Skip((offset - 1) * limit)
                            .Limit(limit).ToList();
                }

                return _collection.Find(filter).ToList();
            }
        }

        public T GetEntityByFilter(FilterDefinition<T> filter)
        {
            return _collection.Find(filter).FirstOrDefault();
        }
    }
}
