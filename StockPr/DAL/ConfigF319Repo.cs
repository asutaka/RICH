﻿using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigF319Repo : IBaseRepo<ConfigF319>
    {
    }

    public class ConfigF319Repo : BaseRepo<ConfigF319>, IConfigF319Repo
    {
        public ConfigF319Repo()
        {
        }
    }
}
