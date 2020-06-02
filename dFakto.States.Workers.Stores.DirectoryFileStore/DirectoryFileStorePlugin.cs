using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Stores.DirectoryFileStore
{
    public class DirectoryStoreStorePlugin : IStorePlugin
    {
        public string Type => DirectoryFileStore.TYPE;
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new DirectoryFileStore(name,configurationSection.Get<DirectoryFileStoreConfig>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}