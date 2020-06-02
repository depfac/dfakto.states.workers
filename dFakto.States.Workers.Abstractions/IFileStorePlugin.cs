using System;
using Microsoft.Extensions.Configuration;

namespace dFakto.States.Workers.Abstractions
{
    public interface IFileStorePlugin : IPlugin
    {
        string Name { get; }

        IFileStore CreateInstance(IServiceProvider serviceProvider, string name, IConfigurationSection configurationSection);
    }
}