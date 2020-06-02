using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Sql
{
    public class SqlInsertFromInputWorkerInput
    {
        public string ConnectionName { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public Dictionary<string,object> Values { get; set; }
    }
    
    public class SqlInsertFromInputWorker : BaseWorker<SqlInsertFromInputWorkerInput,string>
    {
        private readonly ILogger<SqlInsertFromInputWorker> _logger;
        private readonly IEnumerable<BaseDatabase> _databases;

        public SqlInsertFromInputWorker(ILogger<SqlInsertFromInputWorker> logger,IEnumerable<BaseDatabase> databases) : base(
            "inputToSql",
            TimeSpan.FromSeconds(30),
            10)
        {
            _logger = logger;
            _databases = databases;
        }

        public override async Task<string> DoWorkAsync(SqlInsertFromInputWorkerInput input, CancellationToken token)
        {
            var database = _databases.FirstOrDefault(x => x.Name == input.ConnectionName);
            if (database == null)
            {
                throw new ArgumentException($"Invalid ConnectionName '{input.ConnectionName}'");
            }

            _logger.LogInformation($"Inserting values from input into {input.ConnectionName}:{input.SchemaName}.{input.TableName}");
            
            using DbConnection connection = database.CreateConnection();
            await connection.OpenAsync(token);
            var cmd = connection.CreateCommand();
                
            StringBuilder query = new StringBuilder("INSERT INTO ");
            if (!string.IsNullOrEmpty(input.SchemaName))
            {
                query.Append(input.SchemaName);
                query.Append('.');
            }

            query.Append(input.TableName);
            query.Append(" (");
            int index = 0;
            foreach (var keyValue in input.Values)
            {
                if (index > 0)
                {
                    query.Append(',');
                }
                query.Append(keyValue.Key);

                var pp = cmd.CreateParameter();
                pp.ParameterName = $"@p_{index}";
                
                var value = (JsonElement?) keyValue.Value ?? new JsonElement();
                switch (value.ValueKind)
                {
                    case JsonValueKind.Null:
                    case JsonValueKind.Array:
                    case JsonValueKind.Object:
                    case JsonValueKind.Undefined:
                        pp.Value = DBNull.Value;
                        break;
                    case JsonValueKind.String:
                        pp.Value = value.GetString();
                        break;
                    case JsonValueKind.Number:
                        pp.Value = value.GetDecimal();
                        break;
                    case JsonValueKind.True:
                        pp.Value = true;
                        break;
                    case JsonValueKind.False:
                        pp.Value = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                cmd.Parameters.Add(pp);
                index++;
            }

            query.Append(") VALUES (");
            for (int i = 0; i < cmd.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    query.Append(",");
                }
                query.Append(cmd.Parameters[i].ParameterName);
            }

            query.Append(");");


            cmd.CommandText = query.ToString();
            _logger.LogDebug($"Query : {cmd.CommandText}");
            await cmd.ExecuteNonQueryAsync(token);
            return cmd.CommandText;
        }
    }
}