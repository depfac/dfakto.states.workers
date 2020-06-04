using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Stores.PostgresqlDatabaseStore
{
    public class PostgresqlDbStorePlugin : IStorePlugin
    {
        public string Type => "postgresql";
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new PostgresqlDbStore(name, configurationSection.Get<NpgsqlConfig>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}