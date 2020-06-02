using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Gzip
{
    public class GzipWorkerPlugin : IWorkerPlugin
    {
        public IWorker CreateInstance(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<GZipWorker>();
        }
        
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IWorker, GZipWorker>();
            serviceCollection.AddTransient<GZipWorker>();
        }
    }
}