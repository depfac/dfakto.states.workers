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
    public class SqlInsertFromJsonArrayWorkerTests : BaseTests
    {
        private const string SampleJson =
            "[\n  {\n    \"id\": \"23203297-b3a1-42b3-a0e6-da6457882a62\",\n    \"firstname\": \"Bob\",\n    \"lastname\": \"Garcia\",\n    \"contract_time\": 35.0,\n    \"contract_type\": \"CDI\",\n    \"location_id\": \"a7a12627-5f26-4dbe-9b05-9bc969722ea1\",\n    \"role\": \"director\",\n    \"hourly_gross_rate\": 27.01,\n    \"start_date\": \"2015-08-03\",\n    \"end_date\": null\n  },\n  {\n    \"id\": \"d101f97e-a918-471f-9bc3-fca1d0cfa780\",\n    \"firstname\": \"Frank\",\n    \"lastname\": \"Dupont\",\n    \"contract_time\": 28.0,\n    \"contract_type\": \"CDI\",\n    \"location_id\": \"a7a98627-5f26-4dbe-9b05-9bc969722ea1\",\n    \"role\": \"employee\",\n    \"hourly_gross_rate\": 13.26,\n    \"start_date\": \"2017-11-27\",\n    \"end_date\": null\n  }]";
        
        [Fact]
        public async Task TestSqlInsertFromJsonArrayWorker()
        {
            var sql = Host.Services.GetService<SqlInsertFromJsonWorker>();

            foreach (var database in Host.Services.GetService<IStoreFactory>().GetFileStores().Where( x => x is IDbStore).Cast<IDbStore>())
            {
                var tableName = CreateTable(database);
                
                var input = new SqlInsertFromJsonWorkerInput();
                input.ConnectionName = database.Name;
                input.TableName = tableName;
                input.Json = JsonDocument.Parse(SampleJson).RootElement;
                input.JsonColumn = "json";
                input.Columns = new Dictionary<string, string>{{"col1","id"},{"col2","contract_time"},{"firstname","firstname"}};
                
                var output = await sql.DoJsonWork<SqlInsertFromJsonWorkerInput,string>(input);
                
                Assert.Equal(2,CountTable(database,tableName));
                
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
                cmd.CommandText = $"CREATE TABLE {tableName} (json VARCHAR(2000), col1 VARCHAR(100),col2 DECIMAL(10,2), firstname VARCHAR(100))";
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