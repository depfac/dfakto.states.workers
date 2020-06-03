using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.Internals;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Stores;
using McMaster.NETCore.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace dFakto.States.Workers.BaseHost
{
    public class Startup
    {
        private readonly List<PluginLoader> _plugins;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;

             _plugins = GetPluginLoaders();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddStores(Configuration.GetSection("Stores").Get<StoreFactoryConfig>());

            ConfigurePlugins(_plugins, services);

            services.AddStepFunctions(Configuration.GetSection("stepFunctions").Get<StepFunctionsConfig>());

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseRouting();
            //app.UseAuthorization();
            app.UseEndpoints(e => e.MapRazorPages());
        }
        
        
        private static List<PluginLoader> GetPluginLoaders()
        {
            var loaders = new List<PluginLoader>();

            // create plugin loaders
            var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");
            
            Log.Logger.Information($"Loading Plugins from directory '{pluginsDir}'");
            
            foreach (var dir in Directory.GetDirectories(pluginsDir))
            {
                var dirName = Path.GetFileName(dir);
                var pluginDll = Path.Combine(dir, dirName + ".dll");
                if (File.Exists(pluginDll))
                {
                    Log.Logger.Information($"Loading Plugin '{pluginDll}'");
                    
                    var loader = PluginLoader.CreateFromAssemblyFile(
                        pluginDll,
                        new[] { 
                            typeof(IPlugin),
                            typeof(ILogger),
                            typeof(IStorePlugin),
                            typeof(IStoreFactory),
                            typeof(StoreFactory),
                            typeof(StoreFactoryConfig),
                            typeof(StoreConfig),
                            typeof(IStore),
                            typeof(IDbStore),
                            typeof(IFileStore),
                            typeof(IServiceCollection),
                            typeof(IServiceProvider)
                        });
                    loaders.Add(loader);
                }
            }

            return loaders;
        }
        
        private static void ConfigurePlugins(IEnumerable<PluginLoader> loaders, IServiceCollection services)
        {
            // Create an instance of plugin types
            foreach (var loader in loaders)
            {
                foreach (var pluginType in loader
                    .LoadDefaultAssembly()
                    .GetTypes().Where(x => !x.IsAbstract))
                {
                    if (typeof(IPlugin).IsAssignableFrom(pluginType))
                    {
                        // This assumes the implementation of IPlugin has a parameterless constructor
                        var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                        plugin?.Configure(services);
                        services.AddSingleton(plugin);
                        
                        switch (pluginType)
                        {
                            case IStorePlugin fileStorePlugin:
                                services.AddSingleton(fileStorePlugin);
                                break;
                        }
                    }

                    if (typeof(IWorker).IsAssignableFrom(pluginType))
                    {
                        services.AddTransient(pluginType);
                        services.AddSingleton<IHostedService>(x => new WorkerHostedService(
                            (IWorker) x.GetService(pluginType),
                            x.GetService<IHeartbeatManager>(),
                            x.GetService<StepFunctionsConfig>(),
                            x.GetService<AmazonStepFunctionsClient>(),
                            x.GetService<ILoggerFactory>()));
                    }
                }
            }
        }
    }
}