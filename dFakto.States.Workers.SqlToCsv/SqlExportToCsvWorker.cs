using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Abstractions.Exceptions;
using dFakto.States.Workers.Abstractions.Sql;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.SqlToCsv
{
    public class SqlExportToCsvInput: SqlQuery
    {
        public string ConnectionName { get; set; }
        public string OutputFileStoreName { get; set; }
        public string OutputFileName { get; set; }
        public char Separator { get; set; } = ';';
        public new SqlQueryType Type { get; set; } = SqlQueryType.Reader;
    }

    public class SqlExportToCsvWorker: BaseWorker<SqlExportToCsvInput, string>
    {
        private readonly ILogger<SqlExportToCsvWorker> _logger;
        private readonly IStoreFactory _storeFactory;

        private static readonly string CsvExtension = "csv";
        
        public SqlExportToCsvWorker(ILogger<SqlExportToCsvWorker> logger, IStoreFactory storeFactory) : 
            base("exportToCsv", TimeSpan.FromSeconds(30), 5)
        {
            _logger = logger;
            _storeFactory = storeFactory;
        }

        public override async Task<string> DoWorkAsync(SqlExportToCsvInput input, CancellationToken token)
        {
            using var connection = CreateConnection(input);
            await OpenConnection(connection, token);
            
            using var reader = await ExecuteQuery(input, connection, token);
            
            using var outputFileStore = _storeFactory.GetFileStoreFromName(input.OutputFileStoreName);
            string outputFileName = GetOutputFileCsvName(input.OutputFileName);
            string outputFileToken = await outputFileStore.CreateFileToken(outputFileName);
            
            using (var streamWriter = await outputFileStore.OpenWrite(outputFileToken))
            using (var writerTmp = new StreamWriter(streamWriter))
            {
                var writer = new CsvStreamWriter(writerTmp, input.Separator) {ForceQuotes = true};
                WriteToCsv(reader, writer);
            }
            return outputFileToken;
        }

        private DbConnection CreateConnection(SqlExportToCsvInput input)
        {
            IDbStore database = GetDataBase(input.ConnectionName, true);
            return database.CreateConnection();
        }

        private async Task<DbDataReader> ExecuteQuery(SqlExportToCsvInput input, DbConnection connection, CancellationToken token)
        {
            SqlQuery query = await RetrieveQuery(input);

            DbCommand command = connection.CreateCommand(query);
            var reader = await command.ExecuteReaderAsync(token);
            return reader;
        }

        private void WriteToCsv(DbDataReader reader, CsvStreamWriter writer)
        {
            var columnNames = GetColumnNames(reader);
            writer.WriteLine(columnNames);
            while (reader.Read())
            {
                writer.WriteLine(reader.LineToStringArray());
            }
        }

        public string[] GetColumnNames(DbDataReader reader)
        {
            return Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToArray();
        }

        private IDbStore GetDataBase(string connectionName, bool throwExceptionIfNotExists = false)
        {
            IDbStore toReturn = _storeFactory.GetDatabaseStoreFromName(connectionName);
            if (toReturn == null && throwExceptionIfNotExists)
            {
                throw new ArgumentException($"Invalid ConnectionName '{connectionName}'");
            }

            return toReturn;
        }

        private async Task OpenConnection(DbConnection connection, CancellationToken token)
        {
            try
            {
                _logger.LogDebug("Opening connection...");
                await connection.OpenAsync(token);
            }
            catch (Exception e)
            {
                throw new SqlConnectionException(e);
            }
        }

        private async Task<SqlQuery> RetrieveQuery(SqlExportToCsvInput input)
        {
            if (input.QueryFileToken != null)
            {
                using var fileStore = _storeFactory.GetFileStoreFromFileToken(input.QueryFileToken);
                using Stream stream = await fileStore.OpenRead(input.QueryFileToken);
                using StreamReader reader = new StreamReader(stream);
                input.Query = reader.ReadToEnd();
            }

            return input;
        }

        private string GetOutputFileCsvName(string outputFileName)
        {
            return $"{outputFileName}.{CsvExtension}";
        }
    }
}