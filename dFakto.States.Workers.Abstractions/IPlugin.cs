using Microsoft.Extensions.DependencyInjection;

namespace dFakto.States.Workers.Abstractions
{
    public interface IPlugin
    {
        void Configure(IServiceCollection serviceCollection);
    }
}