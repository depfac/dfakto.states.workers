using System;
using Amazon;
using Amazon.Runtime;
using Amazon.StepFunctions;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.States.Workers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStepFunctions(this IServiceCollection services,
            StepFunctionsConfig stepFunctionsConfig,
            FileStoreFactoryConfig fileStoreFactoryConfig,
            Action<StepFunctionsBuilder> builder)
        {
            var sfb = new StepFunctionsBuilder(services, stepFunctionsConfig, fileStoreFactoryConfig);
            builder(sfb);
            services.AddSingleton<HeartbeatHostedService>();
            services.AddTransient<IHeartbeatManager>(x => x.GetService<HeartbeatHostedService>());
            services.AddTransient<IHostedService>(x => x.GetService<HeartbeatHostedService>());
            services.AddSingleton(sfb.Config);
            services.AddTransient(GetAmazonStepFunctionsClient);

            return services;
        }

        private static AmazonStepFunctionsClient GetAmazonStepFunctionsClient(IServiceProvider x)
        {
            var config = x.GetService<StepFunctionsConfig>();

            if (string.IsNullOrWhiteSpace(config.AuthenticationKey) ||
                string.IsNullOrWhiteSpace(config.AuthenticationSecret))
            {
                throw new Exception("Missing Step Functions AuthenticationKey and Secret in configuration");
            }

            var credentials = new BasicAWSCredentials(
                config.AuthenticationKey,
                config.AuthenticationSecret);

            var stepFunctionEnvironmentConfig = new AmazonStepFunctionsConfig
            {
                RegionEndpoint = GetAwsRegionEndpoint(config)
            };

            if (!string.IsNullOrEmpty(config.ServiceUrl))
            {
                stepFunctionEnvironmentConfig.ServiceURL = config.ServiceUrl;
            }

            return new AmazonStepFunctionsClient(credentials, stepFunctionEnvironmentConfig);
        }

        private static RegionEndpoint GetAwsRegionEndpoint(StepFunctionsConfig config)
        {
            var regionEndpoint = string.IsNullOrEmpty(config.AwsRegion)
                ? RegionEndpoint.EUWest1
                : RegionEndpoint.GetBySystemName(config.AwsRegion);
            return regionEndpoint;
        }
    }
}