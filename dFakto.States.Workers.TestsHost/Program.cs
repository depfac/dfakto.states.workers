using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.TestsHost
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            bool isService = !(Debugger.IsAttached || (args != null && args.Contains("--console")));

            if (isService)
            {
                string pathToExe = Process.GetCurrentProcess().MainModule?.FileName;
                string pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
            }

            var builder = WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", true);
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args.Where(arg => arg != "--console").ToArray());
                    }
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .UseSentry()
                .UseStartup<Startup>();
                
            var host = builder.Build();
            if (isService)
            {
                host.RunAsService();
            }
            else
            {
                host.Run();
            }
        }
    }
}