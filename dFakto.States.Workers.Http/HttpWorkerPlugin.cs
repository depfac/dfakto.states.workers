using System;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Http
{
    public class HttpWorkerPlugin : IWorkerPlugin
    {
        public IWorker CreateInstance(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<HttpWorker>();
        }
        
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IWorker, HttpWorker>();
            serviceCollection.AddTransient<HttpWorker>();
        }
    }
}