using System;
using System.Collections.Generic;
using System.Linq;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.FileStores
{
    public class StoreFactory : IFileStoreFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly StoreFactoryConfig _config;
        private readonly List<IStorePlugin> _availablePlugins;

        public StoreFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _config = _serviceProvider.GetService<StoreFactoryConfig>();
            _availablePlugins = _serviceProvider.GetServices<IStorePlugin>().ToList();
        }

        private IStore GetStoreFromName(string name)
        {
            var fileStoreConfig = _config.Stores.First(x =>
                string.Compare(x.Name, name, StringComparison.CurrentCultureIgnoreCase) == 0);
            
            if(fileStoreConfig == null)
                throw new ArgumentException($"No store named '{name}'");
            
            var store = GetStore(fileStoreConfig);
            if(store == null)
                throw new ArgumentException($"No Store of type '{fileStoreConfig.Type}' found");

            return store;
        }

        public IDbStore GetDatabaseStoreFromName(string name)
        {
            var store = GetStoreFromName(name);
            
            if (store is IDbStore databaseStore)
                return databaseStore;
            
            throw new ArgumentException($"The store named '{name}' is not a IDatabaseStore");
        }

        public IFileStore GetFileStoreFromName(string name)
        {
            var store = GetStoreFromName(name);
            
            if (store is IFileStore fileStore)
                return fileStore;
            
            throw new ArgumentException($"The store named '{name}' is not a IFileStore");
        }

        public IFileStore GetFileStoreFromFileToken(string fileToken)
        {
            var storeName = FileToken.ParseName(fileToken);
            return GetFileStoreFromName(storeName);
        }

        public IEnumerable<IStore> GetFileStores()
        {
            foreach (var fileStoreConfig in _config.Stores)
            {
                yield return GetStore(fileStoreConfig);
            }
        }
        
        private IStore GetStore(StoreConfig configuration)
        {
            return _availablePlugins
                .FirstOrDefault(x => x.Type == configuration.Type)
                ?.CreateInstance(_serviceProvider,configuration.Name,configuration.Config);
        }

    }
}