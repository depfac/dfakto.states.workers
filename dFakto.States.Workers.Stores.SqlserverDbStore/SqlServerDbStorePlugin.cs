using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Stores.SqlserverDbStore
{
    public class SqlServerDbStorePlugin : IStorePlugin
    {
        public string Type => "sqlserver";
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new SqlServerDbStore(
                name, 
                configurationSection.Get<SqlServerConfig>(), 
                serviceProvider.GetService<ILogger<SqlServerDbStore>>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}