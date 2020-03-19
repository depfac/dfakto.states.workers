using System;
using System.Collections.Generic;
using System.IO;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.FileStores.File;
using dFakto.States.Workers.FileStores.Ftp;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace dFakto.States.Workers.Tests
{
    public class BaseTests : IDisposable
    {
        protected readonly IHost Host;
        private readonly string _path = Path.Combine(Path.GetTempPath(), DateTime.Now.Ticks.ToString());

        public BaseTests()
        {
            Host = CreateHost(_path);
        }
        
        public static IHost CreateHost(string tempStorePath)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services)  =>
                {
                    var config = new ConfigurationBuilder()
                        .AddInMemoryCollection(new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("test:basePath", Path.Combine(Path.GetTempPath(),"utests")),
                        })
                        .Build();
                    
                    services.AddStepFunctions(new StepFunctionsConfig
                    {
                        AuthenticationKey = "KEY",
                        AuthenticationSecret = "SECRET"
                    },new FileStoreFactoryConfig
                        {
                            Stores = new []
                            {
                                new FileStoreConfig
                                {
                                    Name = "test",
                                    Type = DirectoryFileStore.TYPE,
                                    Config = config.GetSection("test")
                                },
                                new FileStoreConfig
                                {
                                    Name = "testftp",
                                    Type = FtpFileStore.TYPE,
                                    Config = config.GetSection("ftptest")
                                }
                            }
                        }, 
                        x =>
                    {
                        DatabaseConfig[] configs = 
                        {
                            new DatabaseConfig
                            {
                                Name = "pgsql",
                                Type = SqlDatabaseType.PostgreSql,
                                ConnectionString =
                                    "server=localhost; user id=postgres; password=depfac$2000; database=test"
                            },
                            new DatabaseConfig
                            {
                                Name = "sqlserver",
                                Type = SqlDatabaseType.SqlServer,
                                ConnectionString = "server=localhost; user id=sa; password=depfac$2000; database=test"
                            },
                            new DatabaseConfig
                            {
                                Name = "mariadb",
                                Type = SqlDatabaseType.MariaDb,
                                ConnectionString = "server=localhost; user id=root; password=depfac$2000; database=test; AllowLoadLocalInfile=true"
                            },
                        };
                        
                        x.AddDirectoryFileStore();
                        x.AddFtpFileStore();
                        
                        x.AddSqlWorkers(configs);
                        x.AddWorker<GZipWorker>();
                        x.AddWorker<HttpWorker>();
                        x.AddWorker<SqlQueryWorker>();
                        x.AddWorker<SqlBulkInsertWorker>();
                        x.AddWorker<SqlInsertFromInputWorker>();
                    });
                });

            return builder.Build();
        }

        public virtual void Dispose()
        {
            Host?.Dispose();
            Directory.CreateDirectory(_path);
        }
    }
}