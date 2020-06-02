using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Config;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Stores.DirectoryFileStore;
using dFakto.States.Workers.Stores.FtpFileStore;
using dFakto.States.Workers.Http;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

                    services.AddFileStores(new StoreFactoryConfig
                    {
                        Stores = new[]
                        {
                            new StoreConfig
                            {
                                Name = "test",
                                Type = DirectoryFileStore.TYPE,
                                Config = config.GetSection("test")
                            },
                            new StoreConfig
                            {
                                Name = "testftp",
                                Type = FtpFileStore.TYPE,
                                Config = config.GetSection("ftptest")
                            }
                        }
                    });

                    //Load plugins statically
                    services.AddSingleton<IStorePlugin>(new DirectoryStoreStorePlugin());
                    services.AddSingleton<IStorePlugin>(new FtpStoreStorePlugin());
                    
                    DatabaseConfig[] configs = 
                    {
                        new DatabaseConfig
                        {
                            Name = "pgsql",
                            Type = "dFakto.States.Workers.Sql-old.PostgreSQL.PostgreSqlBaseDatabase, dFakto.States.Workers.Sql-old",
                            ConnectionString = "server=localhost; user id=postgres; password=depfac$2000; database=test"
                        },
                        new DatabaseConfig
                        {
                            Name = "sqlserver",
                            Type = "dFakto.States.Workers.Sql-old.SQLServer.SqlServerBaseDatabase, dFakto.States.Workers.Sql-old",
                            ConnectionString = "server=localhost; user id=sa; password=depfac$2000; database=test"
                        },
                        new DatabaseConfig
                        {
                            Name = "mariadb",
                            Type = "dFakto.States.Workers.Sql-old.MySQL.MySqlDatabase, dFakto.States.Workers.Sql-old",
                            ConnectionString = "server=localhost; user id=root; password=depfac$2000; database=test; AllowLoadLocalInfile=true"
                        },
                        new DatabaseConfig
                        {
                            Name = "oracle",
                            Type = "dFakto.States.Workers.Sql-old.Oracle.OracleDatabase, dFakto.States.Workers.Sql-old",
                            ConnectionString = "User Id=root; Password=depfac$2000; Data Source=localhost:1521/orc1"
                        },
                    };
                    
                    services.AddStepFunctions(new StepFunctionsConfig
                    {
                        AuthenticationKey = "KEY",
                        AuthenticationSecret = "SECRET"
                    });
                });

            return builder.Build();
        }
        
        protected void CreateTable(string tableName)
        {
            foreach (var database in Host.Services.GetServices<BaseDatabase>())
            {
                var conn = database.CreateConnection();
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"CREATE TABLE {tableName} (col1 INT , col2 VARCHAR(100))";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        protected void Insert(string tableName, params (int,string)[] values)
        {
            foreach (var database in Host.Services.GetServices<BaseDatabase>())
            {
                foreach (var value in values)
                {
                    var conn = database.CreateConnection();
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"INSERT INTO {tableName} VALUES(@p1, @p2)";
                        var p = cmd.CreateParameter();
                        p.ParameterName = "p1";
                        p.Value = value.Item1;
                        cmd.Parameters.Add(p);
                        
                        var p2 = cmd.CreateParameter();
                        p2.ParameterName = "p2";
                        p2.Value = value.Item2;
                        cmd.Parameters.Add(p2);

                        cmd.ExecuteNonQuery();
                    }
                }

            }
        }
        
        public async Task<string> ReadTextFileInStore(IFileStore fileStore, string fileToken)
        {
            await using var stream = await fileStore.OpenRead(fileToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public virtual void Dispose()
        {
            Host?.Dispose();
            Directory.CreateDirectory(_path);
        }
        
    }
}