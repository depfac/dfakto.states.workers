using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.FileStores.DirectoryFileStore
{
    public class DirectoryFileStoreFileStorePlugin : IFileStorePlugin
    {
        public string Name => DirectoryFileStore.TYPE;
        
        public IFileStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new DirectoryFileStore(name,configurationSection.Get<DirectoryFileStoreConfig>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}