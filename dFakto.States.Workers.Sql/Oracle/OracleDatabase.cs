using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Sql.Common;
using Oracle.ManagedDataAccess.Client;

namespace dFakto.States.Workers.Sql.Oracle
{
    public class OracleDatabase : BaseDatabase
    {
        public OracleDatabase(DatabaseConfig config) : base(config)
        {
        }

        public override DbConnection CreateConnection()
        {
            return new OracleConnection(Config.ConnectionString);
        }

        public override Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}