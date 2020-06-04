using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using dFakto.States.Workers.Models;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.States.Workers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HomeController(ILogger<HomeController> logger,IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IActionResult Index()
        {
            List<object> workers = new List<object>(); 
            
            foreach (var service in _serviceProvider.GetServices<IHostedService>())
            {
                if (service is WorkerHostedService whs)
                {
                    var t = whs.Worker.GetType().BaseType;
                    var rr = t.GenericTypeArguments[0];
                    workers.Add(rr.FullName);
                }
            }

            foreach (var plu in _serviceProvider.GetServices<IPlugin>())
            {
                workers.Add(plu.GetType().FullName);
            }
            
            
            return View(new IndexModel
            {
                Plugins = new List<object>(workers)
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}