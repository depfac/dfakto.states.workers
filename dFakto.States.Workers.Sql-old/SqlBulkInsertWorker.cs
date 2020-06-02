using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Sql.Common;
using dFakto.States.Workers.Sql.Csv;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Sql
{
    public class SqlBulkInsertWorker : BaseWorker<BulkInsertInput,bool>
    {
        private readonly ILogger<SqlBulkInsertWorker> _logger;
        private readonly IEnumerable<BaseDatabase> _databases;
        private readonly IStoreFactory _storeFactory;

        public SqlBulkInsertWorker(ILogger<SqlBulkInsertWorker> logger, IEnumerable<BaseDatabase> databases, IStoreFactory storeFactory) : base("SQLBulkInsert")
        {
            _logger = logger;
            _databases = databases;
            _storeFactory = storeFactory;
        }

        public override async Task<bool> DoWorkAsync(BulkInsertInput input, CancellationToken token)
        {
            string tmpFileName = null;
            
            var destinationDatabase = _databases.FirstOrDefault(x => x.Name == input.Destination.ConnectionName);
            if (destinationDatabase == null)
            {
                throw new ArgumentException("Invalid Destination ConnectionName");
            }

            if (input.Destination.TruncateFirst)
            {
                await destinationDatabase.TruncateTable(input.Destination.SchemaName, input.Destination.TableName);
            }

            DbConnection sourceConnection = null;
            DbCommand sourceCommand = null;
            IDataReader dataReader = null;
            
            if (!string.IsNullOrEmpty(input.Source.ConnectionName))
            {
                _logger.LogDebug($"Quering '{input.Source.ConnectionName}' as source");
                var sourceDatabase = _databases.FirstOrDefault(x => x.Name == input.Source.ConnectionName);
                if (sourceDatabase == null)
                {
                    throw new ArgumentException("Invalid Source ConnectionName");
                }
                
                if (input.Source.Query.QueryFileToken != null)
                {
                    using(var fileStore = _storeFactory.GetFileStoreFromFileToken(input.Source.Query.QueryFileToken))
                    using(Stream stream = await fileStore.OpenRead(input.Source.Query.QueryFileToken))
                    using(StreamReader reader = new StreamReader(stream))
                    {
                        input.Source.Query.Query = await reader.ReadToEndAsync();
                    }
                }
                
                sourceConnection = sourceDatabase.CreateConnection();
                await sourceConnection.OpenAsync(token);
                sourceCommand = sourceConnection.CreateCommand(input.Source.Query);
                dataReader = await sourceCommand.ExecuteReaderAsync(token);
            }
            else if(input.Source.FileToken != null)
            {
                using var fileStore = _storeFactory.GetFileStoreFromFileToken(input.Source.FileToken);
                tmpFileName = Path.GetTempFileName();
                
                _logger.LogDebug($"Copying filetoken '{input.Source.FileToken}' into '{tmpFileName}'");
                using (var reader = await fileStore.OpenRead(input.Source.FileToken))
                using (var writer = File.OpenWrite(tmpFileName))
                {
                    await reader.CopyToAsync(writer,2048, token);
                }

                input.Source.FileName = tmpFileName;
            }
            
            if(dataReader == null)
            {
                if (string.IsNullOrEmpty(input.Source.FileName))
                {
                    throw new ArgumentException("No Source specified");
                }
                if (!File.Exists(input.Source.FileName))
                {
                    throw new ArgumentException($"File '{input.Source.FileName}' not found");
                }
                
                _logger.LogDebug($"Loading file '{input.Source.FileName}' as source");
                
                dataReader = new CsvDataReader(input.Source.FileName,input.Source.Separator,input.Source.Headers, CultureInfo.GetCultureInfo(input.Source.CultureName));
            }

            try
            {
                _logger.LogDebug("Starting BulkInsert");
                await destinationDatabase.BulkInsert(
                    dataReader,
                    input.Destination.SchemaName,
                    input.Destination.TableName,
                    input.Destination.Timeout,
                    token);
                
                _logger.LogDebug("BulkInsert completed");

            }
            finally
            {
                dataReader?.Dispose();
                sourceCommand?.Dispose();
                sourceConnection?.Dispose();
            }
            
            if (!String.IsNullOrEmpty(tmpFileName))
            {
                try
                {
                    File.Delete(input.Source.FileName);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e,"Error while deleting Temp File Name");
                }
            }
            
            return true;
        }
    }
}