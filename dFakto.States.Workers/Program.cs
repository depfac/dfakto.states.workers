using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace dFakto.States.Workers
{
    public class Program
    {
        public static int Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            bool isService = !(Debugger.IsAttached || (args != null && args.Contains("--console")));
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            
            
            var builder = CreateHostBuilder(args);
            
            if (isService)
            {
                Log.Information("Starting as windows service");
                
                string pathToExe = Process.GetCurrentProcess().MainModule?.FileName;
                string pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
                
                
                builder.UseWindowsService();
            }
            else
            {
                Log.Information("Starting as console application");
                builder.UseConsoleLifetime();
            }
            
            try
            {
                builder.Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSentry();
                    webBuilder.UseSerilog();
                    webBuilder.UseStartup<Startup>();
                });
    }
}