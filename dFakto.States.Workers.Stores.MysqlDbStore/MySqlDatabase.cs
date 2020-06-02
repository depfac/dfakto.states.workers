using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace dFakto.States.Workers.Stores.MysqlDbStore
{
    public class MysqlDbStore : IDbStore
    {
        private readonly string _name;
        private readonly MysqlConfig _config;
        private readonly ILogger<MysqlDbStore> _logger;

        public MysqlDbStore(string name, MysqlConfig config, ILogger<MysqlDbStore> logger)
        {
            _name = name;
            _config = config;
            _logger = logger;
        }
        
        public DbConnection CreateConnection()
        {
            //MySQL Server, the connection string must have AllowLoadLocalInfile=true
            return new MySqlConnection(_config.ConnectionString);
        }

        public async Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            await using var conn = new MySqlConnection(_config.ConnectionString);
            
            var bulkCopy = new MySqlBulkCopy(conn);
            bulkCopy.BulkCopyTimeout = timeout;
            bulkCopy.DestinationTableName = tableName;
            await bulkCopy.WriteToServerAsync(reader, token);
        }
    }
}