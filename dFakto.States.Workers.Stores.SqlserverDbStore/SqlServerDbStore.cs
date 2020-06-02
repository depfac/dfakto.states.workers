using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Stores.SqlserverDbStore
{
    internal class SqlServerDbStore : IDbStore
    {
        private readonly string _name;
        private readonly SqlServerConfig _config;

        public SqlServerDbStore(string name, SqlServerConfig config, ILogger<SqlServerDbStore> logger)
        {
            _name = name;
            _config = config;
        }
        
        public DbConnection CreateConnection()
        {
            return new SqlConnection(_config.ConnectionString);
        }

        public async Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            using var conn = new SqlConnection(_config.ConnectionString);
            await conn.OpenAsync(token);
            
            SqlBulkCopy bulk = new SqlBulkCopy(conn,SqlBulkCopyOptions.TableLock |
                                                 SqlBulkCopyOptions.FireTriggers |
                                                 SqlBulkCopyOptions.UseInternalTransaction,null);
            bulk.DestinationTableName = string.IsNullOrEmpty(schemaName) ? tableName : schemaName + "." + tableName;
            bulk.EnableStreaming = true;
            bulk.BulkCopyTimeout = timeout;
            await bulk.WriteToServerAsync(reader, token);

        }
    }
}