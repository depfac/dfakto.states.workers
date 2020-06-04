using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Gzip;
using dFakto.States.Workers.Stores.DirectoryFileStore;
using dFakto.States.Workers.Stores.FtpFileStore;
using dFakto.States.Workers.Http;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using dFakto.States.Workers.SqlQuery;
using dFakto.States.Workers.Stores;
using dFakto.States.Workers.Stores.MysqlDbStore;
using dFakto.States.Workers.Stores.OracleDbStore;
using dFakto.States.Workers.Stores.PostgresqlDatabaseStore;
using dFakto.States.Workers.Stores.SqlserverDbStore;
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

                    services.AddSingleton<IStoreFactory, TestStoreFactory>();
                    
                    //Load plugins statically
                    services.AddSingleton<IStorePlugin>(new DirectoryStoreStorePlugin());
                    services.AddSingleton<IStorePlugin>(new FtpStoreStorePlugin());
                    services.AddSingleton<IStorePlugin>(new PostgresqlDbStorePlugin());
                    services.AddSingleton<IStorePlugin>(new OracleDbStorePlugin());
                    services.AddSingleton<IStorePlugin>(new SqlServerDbStorePlugin());
                    services.AddSingleton<IStorePlugin>(new MysqlDbStorePlugin());


                    services.AddTransient<GZipWorker>();
                    services.AddTransient<HttpWorker>();
                    services.AddTransient<SqlBulkInsert.SqlBulkInsertWorker>();
                    services.AddTransient<SqlQueryWorker>();
                    services.AddTransient<SqlToCsv.SqlExportToCsvWorker>();
                    services.AddTransient<SqlInsertFromJson.SqlInsertFromJsonWorker>();
                });

            return builder.Build();
        }
        
        protected void CreateTable(string tableName)
        {
            foreach (var database in Host.Services.GetService<IStoreFactory>().GetStores().Where(x => x is IDbStore).Cast<IDbStore>())
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
            foreach (var database in Host.Services.GetService<IStoreFactory>().GetStores().Where(x => x is IDbStore).Cast<IDbStore>())
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