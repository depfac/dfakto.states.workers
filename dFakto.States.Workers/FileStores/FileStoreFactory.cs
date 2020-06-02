using System;
using System.Collections.Generic;
using System.Linq;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.FileStores
{
    public class FileStoreFactory : IFileStoreFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FileStoreFactoryConfig _config;

        public FileStoreFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _config = _serviceProvider.GetService<FileStoreFactoryConfig>();
        }

        public IFileStore GetFileStoreFromName(string name)
        {
            var fileStoreConfig = _config.Stores.First(x =>
                string.Compare(x.Name, name, StringComparison.CurrentCultureIgnoreCase) == 0);

            return GetFileStore(fileStoreConfig);
        }

        public IFileStore GetFileStoreFromFileToken(string fileToken)
        {
            var storeName = FileToken.ParseName(fileToken);

            var fileStoreConfig = _config.Stores.FirstOrDefault(x =>
                string.Compare(x.Name, storeName, StringComparison.CurrentCultureIgnoreCase) == 0);

            if(fileStoreConfig == null)
                throw new ArgumentException($"No File store named '{storeName}'");
            
            return GetFileStore(fileStoreConfig);
        }

        public IEnumerable<IFileStore> GetFileStores()
        {
            foreach (var fileStoreConfig in _config.Stores)
            {
                yield return GetFileStore(fileStoreConfig);
            }
        }
        
        private IFileStore GetFileStore(FileStoreConfig configuration)
        {
            return _serviceProvider.GetServices<IFileStorePlugin>()
                .FirstOrDefault(x => x.Name == configuration.Type)
                ?.CreateInstance(_serviceProvider,configuration.Name,configuration.Config);
        }

    }
}