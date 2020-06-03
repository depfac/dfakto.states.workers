using System.Data;
using System.Data.Common;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace dFakto.States.Workers.Abstractions
{
    public interface IDbStore : IStore
    {
        DbConnection CreateConnection();

        DbParameter CreateJsonParameter(DbCommand command, string parameterName, string value); 
        
        Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token);

        public async Task TruncateTable(string schemaName, string tableName)
        {
            string fullTableName = string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";

            await using var conn = CreateConnection();
            await conn.OpenAsync();
            
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "TRUNCATE TABLE " + fullTableName;
            await cmd.ExecuteNonQueryAsync();
        }
        
    }
}