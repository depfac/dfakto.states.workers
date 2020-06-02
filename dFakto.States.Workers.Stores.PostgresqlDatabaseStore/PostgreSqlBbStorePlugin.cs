using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Stores.PostgresqlDatabaseStore
{
    public class PostgreSqlBbStorePlugin : IStorePlugin
    {
        public string Type => "pgsql";
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new PostgreSqlBbStore(name, configurationSection.Get<NpgsqlConfig>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}