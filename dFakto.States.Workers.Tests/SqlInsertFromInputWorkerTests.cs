using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using dFakto.States.Workers.SqlInsertFromJson;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dFakto.States.Workers.Tests
{
    public class SqlInsertFromInputWorkerTests : BaseTests
    {
        [Fact]
        public async Task TestSqlInsertFromInputWorker()
        {
            var sql = Host.Services.GetService<SqlInsertFromJsonWorker>();
            
            foreach (var database in Host.Services.GetService<IStoreFactory>().GetFileStores().Where( x => x is IDbStore).Cast<IDbStore>())
            {
                var tableName = CreateTable(database);
                
                var input = new SqlInsertFromJsonWorkerInput();
                input.ConnectionName = database.Name;
                input.TableName = tableName;
                input.Json = JsonDocument.Parse("{\"col1\":23,\"col2\":\"COUCOU\",\"col3\":null}").RootElement;
                var output = await sql.DoJsonWork<SqlInsertFromJsonWorkerInput,string>(input);
                
                Assert.Equal(1,CountTable(database,tableName));
                
                DropTable(database, tableName);
            }
        }
        
        private string CreateTable(IDbStore database)
        {
            var tableName = StringUtils.Random(10);
            using var conn = database.CreateConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"CREATE TABLE {tableName} (col1 INT,col2 VARCHAR(100),col3 VARCHAR(100))";
                cmd.ExecuteNonQuery();
            }

            return tableName;
        }
        
        private void DropTable(IDbStore database, string tableName)
        {
            using var conn = database.CreateConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"DROP TABLE {tableName}";
                cmd.ExecuteNonQuery();
            }
        }
        
        private long CountTable(IDbStore database, string tableName)
        {
            using var conn = database.CreateConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
                var r = cmd.ExecuteScalar();
                if (r is long)
                {
                    return (long) r;
                }

                return (int) r;
            }
        }
    }
}