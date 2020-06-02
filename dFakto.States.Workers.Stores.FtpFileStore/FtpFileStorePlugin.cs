using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Stores.FtpFileStore
{
    public class FtpStoreStorePlugin : IStorePlugin
    {
        public string Type => FtpFileStore.TYPE;
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new FtpFileStore(name,configurationSection.Get<FtpFileStoreConfig>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}