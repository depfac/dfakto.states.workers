using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.SqlInsertFromJson
{
    public class SqlInsertFromJsonArrayWorkerInput
    {
        public string ConnectionName { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string FileToken { get; set; }
        public JsonElement Json { get; set; }
        public string JsonColumn { get; set; }
        public Dictionary<string,string> Columns { get; set; }
    }
    
    public class SqlInsertFromJsonArrayWorker : BaseWorker<SqlInsertFromJsonArrayWorkerInput,string>
    {
        private readonly ILogger<SqlInsertFromJsonArrayWorker> _logger;
        private readonly IStoreFactory _storeFactory;

        public SqlInsertFromJsonArrayWorker(ILogger<SqlInsertFromJsonArrayWorker> logger, IStoreFactory storeFactory) : base(
            "sqlInsertFromJson",
            TimeSpan.FromSeconds(30),
            10)
        {
            _logger = logger;
            _storeFactory = storeFactory;
        }

        public override async Task<string> DoWorkAsync(SqlInsertFromJsonArrayWorkerInput input, CancellationToken token)
        {
            var database = _storeFactory.GetDatabaseStoreFromName(input.ConnectionName);
            if (database == null)
            {
                throw new ArgumentException($"Invalid ConnectionName '{input.ConnectionName}'");
            }

            if (!string.IsNullOrEmpty(input.FileToken))
            {
                var fileStore = _storeFactory.GetFileStoreFromFileToken(input.FileToken);
                if(fileStore == null)
                {
                    throw new ArgumentException($"Invalid FileToken '{input.FileToken}'");
                }

                using (var stream = await fileStore.OpenRead(input.FileToken))
                {
                    JsonDocument dd = await JsonDocument.ParseAsync(stream, cancellationToken: token);
                    input.Json = dd.RootElement;
                }
            }

            var values = new List<JsonElement>();
            
            switch (input.Json.ValueKind)
            {
                case JsonValueKind.Array:
                    values.AddRange(input.Json.EnumerateArray());
                    break;
                case JsonValueKind.Object:
                    values.Add(input.Json);
                    break;
                default:
                    throw new ArgumentException($"Invalid Input Json '{input.Json.ValueKind}', must be an Array or an Object");
            }
            
            _logger.LogInformation($"Inserting {values.Count} value(s) into {input.ConnectionName}:{input.SchemaName}.{input.TableName}");

            await using DbConnection connection = database.CreateConnection();
            await connection.OpenAsync(token);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = GetInsertQuery(input);
            
            foreach (var keyValue in values)
            {
                cmd.Parameters.Clear();
                
                if (!string.IsNullOrEmpty(input.JsonColumn))
                {
                    var jsonP = database.CreateJsonParameter(cmd, "@js", keyValue.ToString());
                    cmd.Parameters.Add(jsonP);
                }
                
                int index = 0;
                foreach (var col in input.Columns)
                {
                    var jsonP = cmd.CreateParameter();
                    jsonP.ParameterName = "@p"+index++;
                    jsonP.Value = GetPropertyValue(keyValue, col.Value);
                    cmd.Parameters.Add(jsonP);
                }
                
                _logger.LogDebug($"Query : {cmd.CommandText}");
                await cmd.ExecuteNonQueryAsync(token);
            }
            
            return cmd.CommandText;
        }

        private static object GetPropertyValue(in JsonElement inputJson, string colValue)
        {
            var p = inputJson.GetProperty(colValue);
            switch (p.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                case JsonValueKind.String:
                    return p.ToString();
                case JsonValueKind.Number:
                    return p.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetInsertQuery(SqlInsertFromJsonArrayWorkerInput input)
        {
            StringBuilder query = new StringBuilder("INSERT INTO ");
            if (!string.IsNullOrEmpty(input.SchemaName))
            {
                query.Append(input.SchemaName);
                query.Append('.');
            }

            query.Append(input.TableName);
            query.Append(" (");

            //Add JsonColumn if exists
            if (!string.IsNullOrEmpty(input.JsonColumn))
            {
                query.Append(input.JsonColumn);
            }

            // Add Other columns if any
            bool first = string.IsNullOrEmpty(input.JsonColumn);
            foreach (var addCOl in input.Columns)
            {
                if (!first)
                {
                    query.Append(',');
                }

                query.Append(addCOl.Key);
                first = false;
            }

            query.Append(") VALUES (");
            if (!string.IsNullOrEmpty(input.JsonColumn))
            {
                query.Append("@js");
            }

            for (int i = 0; i < input.Columns.Count; i++)
            {
                if (!string.IsNullOrEmpty(input.JsonColumn) || i > 0)
                {
                    query.Append(",");
                }

                query.Append("@p" + i);
            }

            query.Append(");");
            return query.ToString();
        }
    }
}