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
        public async Task TestScalarQuery()
        {
            var sql = Host.Services.GetService<SqlInsertFromInputWorker>();

            foreach (var database in Host.Services.GetServices<BaseDatabase>())
            {
                var tableName = CreateTable(database);
                
                var input = new SqlInsertFromInputWorkerInput();
                input.ConnectionName = database.Name;
                input.TableName = tableName;
                input.Values = new Dictionary<string, object>(){{"col1",23},{"col2","COUCOU"}};
                var output = await sql.DoJsonWork<SqlInsertFromInputWorkerInput,string>(input);
                
            }
        }
        
        private string CreateTable(BaseDatabase database)
        {
            var tableName = StringUtils.Random(10);
            var conn = database.CreateConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"CREATE TABLE {tableName} (col1 INT , col2 VARCHAR(100))";
                cmd.ExecuteNonQuery();
            }

            return tableName;
        }
    }
}