using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Sql
{
    public class WorkerInput
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public Dictionary<string,object> Values { get; set; }
    }
    
    public class SqlInsertFromInputWorker : BaseWorker<WorkerInput,string>
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

        public override async Task<string> DoWorkAsync(WorkerInput input, CancellationToken token)
        {
            var database = _databases.FirstOrDefault(x => x.Name == input.DatabaseName);
            if (database == null)
            {
                throw new ArgumentException("Invalid ConnectionName");
            }

            _logger.LogInformation($"Inserting values from input into {input.DatabaseName}:{input.SchemaName}.{input.TableName}");
            
            using DbConnection connection = database.CreateConnection();
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
            foreach (var value in input.Values)
            {
                if (index > 0)
                {
                    query.Append(',');
                }
                query.Append(value.Key);

                var pp = cmd.CreateParameter();
                pp.ParameterName = $"@p_{index}";
                pp.Value = value.Value;
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