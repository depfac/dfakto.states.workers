using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Sql.Common;
using MySql.Data.MySqlClient;

namespace dFakto.States.Workers.Sql.MySQL
{
    public class MySqlDatabase : BaseDatabase
    {
        public override DbConnection CreateConnection()
        {
            //MySQL Server, the connection string must have AllowLoadLocalInfile=true
            return new MySqlConnection(Config.ConnectionString);
        }

        public override async Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            using (var conn = new MySqlConnection(Config.ConnectionString))
            {
                var bulkCopy = new MySqlBulkCopy(conn);
                bulkCopy.BulkCopyTimeout = timeout;
                bulkCopy.DestinationTableName = tableName;
                await bulkCopy.WriteToServerAsync(reader, token);
            }
        }
    }
}