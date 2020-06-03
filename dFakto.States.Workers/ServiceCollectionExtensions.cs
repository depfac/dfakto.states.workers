using System;
using System.Net.Http;
using Amazon;
using Amazon.Runtime;
using Amazon.StepFunctions;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.States.Workers
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// HttpCiientFactory that disable SSL certificate check (use with self-signed certificates) 
        /// </summary>
        private class NoCertificateCheckHttpClientFactory : HttpClientFactory
        {
            private readonly HttpClientHandler _handler = new HttpClientHandler();

            public NoCertificateCheckHttpClientFactory()
            {
                _handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            public override HttpClient CreateHttpClient(IClientConfig clientConfig)
            {
                return new HttpClient(_handler);
            }

            public override bool DisposeHttpClientsAfterUse(IClientConfig clientConfig)
            {
                return true;
            }
        }
        
        public static IServiceCollection AddStepFunctions(this IServiceCollection services,
            StepFunctionsConfig stepFunctionsConfig)
        {
            services.AddSingleton<HeartbeatHostedService>();
            services.AddTransient<IHeartbeatManager>(x => x.GetService<HeartbeatHostedService>());
            services.AddTransient<IHostedService>(x => x.GetService<HeartbeatHostedService>());
            services.AddSingleton(stepFunctionsConfig);
            services.AddTransient(GetAmazonStepFunctionsClient);
            
            return services;
        }

        public static IServiceCollection AddStores(this IServiceCollection services,
            StoreFactoryConfig storeFactoryConfig)
        {
            services.AddSingleton(storeFactoryConfig);
            services.AddSingleton<IStoreFactory>(x => new StoreFactory(x));
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
                if (config.IgnoreSelfSignedCertificates)
                {
                    stepFunctionEnvironmentConfig.HttpClientFactory = new NoCertificateCheckHttpClientFactory();
                }
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