using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace dFakto.States.Workers.Abstractions
{
    public interface IStorePlugin : IPlugin
    {
        string Type { get; }

        IStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection);
    }
}