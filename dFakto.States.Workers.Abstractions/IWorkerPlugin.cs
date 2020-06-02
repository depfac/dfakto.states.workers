using System;

namespace dFakto.States.Workers.Abstractions
{
    public interface IWorkerPlugin : IPlugin
    {
        IWorker CreateInstance(IServiceProvider serviceProvider);
    }
}