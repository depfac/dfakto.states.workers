using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Data.SqlClient;

namespace dFakto.States.Workers.Sql.SQLServer
{
    internal class SqlServerBaseDatabase : BaseDatabase
    {
        public override DbConnection CreateConnection()
        {
            return new SqlConnection(Config.ConnectionString);
        }

        public override async Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            using var conn = new SqlConnection(Config.ConnectionString);
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