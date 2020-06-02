using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace dFakto.States.Workers.Stores.OracleDbStore
{
    public class OracleDbStore : IDbStore
    {
        private readonly string _name;
        private readonly OracleConfig _config;
        private readonly ILogger<OracleDbStore> _logger;

        public OracleDbStore(string name, OracleConfig config, ILogger<OracleDbStore> logger)
        {
            _name = name;
            _config = config;
            _logger = logger;
        }
        
        public DbConnection CreateConnection()
        {
            return new OracleConnection(_config.ConnectionString);
        }

        public Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}