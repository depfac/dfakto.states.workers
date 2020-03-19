using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dFakto.States.Workers.Tests
{
    public class SqlInsertFromInputWorkerTests : BaseTests
    {
        [Fact]
        public async Task TestSqlInsertFromInputWorker()
        {
            var sql = Host.Services.GetService<SqlInsertFromInputWorker>();

            foreach (var database in Host.Services.GetServices<BaseDatabase>())
            {
                var tableName = CreateTable(database);
                
                var input = new SqlInsertFromInputWorkerInput();
                input.ConnectionName = database.Name;
                input.TableName = tableName;
                input.Values = new Dictionary<string, object>(){{"col1",23},{"col2","COUCOU"},{"col3",null}};
                var output = await sql.DoJsonWork<SqlInsertFromInputWorkerInput,string>(input);
                
                Assert.Equal(1,CountTable(database,tableName));
                
                DropTable(database, tableName);
            }
        }
        
        private string CreateTable(BaseDatabase database)
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
        
        private void DropTable(BaseDatabase database, string tableName)
        {
            using var conn = database.CreateConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"DROP TABLE {tableName}";
                cmd.ExecuteNonQuery();
            }
        }
        
        private long CountTable(BaseDatabase database, string tableName)
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