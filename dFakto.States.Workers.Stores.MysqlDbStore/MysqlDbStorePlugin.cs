using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Stores.MysqlDbStore
{
    public class MysqmDbStorePlugin : IStorePlugin
    {
        public string Type => "pgsql";
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new MysqlDbStore(
                name, 
                configurationSection.Get<MysqlConfig>(), 
                serviceProvider.GetService<ILogger<MysqlDbStore>>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}