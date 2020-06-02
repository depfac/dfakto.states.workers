using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.FileStores.FtpFileStore
{
    public class FtpFileStoreFileStorePlugin : IFileStorePlugin
    {
        public string Name => FtpFileStore.TYPE;
        
        public IFileStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new FtpFileStore(name,configurationSection.Get<FtpFileStoreConfig>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}