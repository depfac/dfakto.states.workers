using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.Internals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers
{
    public class StepFunctionsBuilder
    {
        public IServiceCollection ServiceCollection { get; }

        internal StepFunctionsBuilder(IServiceCollection serviceCollectionCollection,
            StepFunctionsConfig stepFunctionsConfig)
        {
            Config = stepFunctionsConfig;
            ServiceCollection = serviceCollectionCollection;

        }

        public StepFunctionsConfig Config { get; }

        /// <summary>
        ///     Add all type implementing IWorker interface in given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public StepFunctionsBuilder AddWorkers(Assembly assembly)
        {
            foreach (var w in assembly.GetTypes().Where(x => typeof(IWorker).IsAssignableFrom(x) && !x.IsAbstract))
            {
                AddWorker(w);
            }

            return this;
        }

        public StepFunctionsBuilder AddWorker(string name, Func<string, Task<string>> worker, int maxConcurrency = 1)
        {
            ServiceCollection.AddSingleton<IHostedService>(x => new WorkerHostedService(
                new FuncWorker(name, worker, maxConcurrency),
                x.GetService<IHeartbeatManager>(),
                x.GetService<StepFunctionsConfig>(), 
                x.GetService<AmazonStepFunctionsClient>(),
                x.GetService<ILoggerFactory>()));
            return this;
        }

        public IServiceCollection AddWorker<T>() where T : class, IWorker
        {
            AddWorker(typeof(T));
            return ServiceCollection;
        }
        
        public IServiceCollection AddWorker(Func<IServiceProvider, IWorker> factory)
        {
            ServiceCollection.AddTransient<IHostedService>(x => new WorkerHostedService(
                factory(x),
                x.GetService<IHeartbeatManager>(),
                x.GetService<StepFunctionsConfig>(),
                x.GetService<AmazonStepFunctionsClient>(),
                x.GetService<ILoggerFactory>()));
            return ServiceCollection;
        }

        public IServiceCollection AddWorker(Type worker)
        {
            ServiceCollection.AddTransient(worker);
            ServiceCollection.AddSingleton<IHostedService>(x =>
            {
                var w = (IWorker) x.GetService(worker);

                return new WorkerHostedService(
                    w,
                    x.GetService<IHeartbeatManager>(),
                    x.GetService<StepFunctionsConfig>(),
                    x.GetService<AmazonStepFunctionsClient>(),
                    x.GetService<ILoggerFactory>());
            });
            return ServiceCollection;
        }
    }
}