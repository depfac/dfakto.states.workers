using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Stores.OracleDbStore
{
    public class OracleDbStorePlugin : IStorePlugin
    {
        public string Type => "pgsql";
        
        public IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection)
        {
            return new OracleDbStore(
                name, 
                configurationSection.Get<OracleConfig>(), 
                serviceProvider.GetService<ILogger<OracleDbStore>>());
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            
        }
    }
}