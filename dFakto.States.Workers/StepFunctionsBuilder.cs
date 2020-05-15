using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.FileStores.File;
using dFakto.States.Workers.FileStores.Ftp;
using dFakto.States.Workers.Interfaces;
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
        private readonly FileStoreFactoryConfig _fileStoreFactoryConfig;
        public IServiceCollection ServiceCollection { get; }

        internal StepFunctionsBuilder(IServiceCollection serviceCollectionCollection,
            StepFunctionsConfig stepFunctionsConfig, 
            FileStoreFactoryConfig fileStoreFactoryConfig)
        {
            _fileStoreFactoryConfig = fileStoreFactoryConfig;
            Config = stepFunctionsConfig;
            ServiceCollection = serviceCollectionCollection;

            ServiceCollection.AddSingleton(fileStoreFactoryConfig);
            ServiceCollection.AddSingleton(x => new FileStoreFactory(x));
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
            ServiceCollection.AddSingleton<IHostedService>(x =>
            {
                return new WorkerHostedService(
                    factory(x),
                    x.GetService<IHeartbeatManager>(),
                    x.GetService<StepFunctionsConfig>(),
                    x.GetService<AmazonStepFunctionsClient>(),
                    x.GetService<ILoggerFactory>());
            });
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

        public void AddFileStore(string type, Func<IServiceProvider, string, IConfigurationSection, IFileStore> factory)
        {
            _fileStoreFactoryConfig.StoreBuilders.Add(type, factory);
        }

        public void AddFtpFileStore()
        {
            _fileStoreFactoryConfig.StoreBuilders.Add(FtpFileStore.TYPE, (provider,  uriBase, config) => new FtpFileStore(uriBase,config.Get<FtpFileStoreConfig>()));
        }
        public void AddDirectoryFileStore()
        {
            _fileStoreFactoryConfig.StoreBuilders.Add(DirectoryFileStore.TYPE, (provider,  name, config) => new DirectoryFileStore(name,config.Get<DirectoryFileStoreConfig>()));
        }
    }
}