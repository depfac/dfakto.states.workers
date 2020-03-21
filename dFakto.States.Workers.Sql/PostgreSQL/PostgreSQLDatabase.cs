using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Sql.Common;
using Npgsql;

namespace dFakto.States.Workers.Sql.PostgreSQL
{
    internal class PostgreSqlBaseDatabase : BaseDatabase
    {

        public PostgreSqlBaseDatabase(DatabaseConfig config)
        :base(config)
        {
        }

        public override DbConnection CreateConnection()
        {
            return new NpgsqlConnection(Config.ConnectionString);
        }

        public override async Task BulkInsert(IDataReader reader, string schemaName, string tableName, int timeout, CancellationToken token)
        {
            await using (var conn = new NpgsqlConnection(Config.ConnectionString))
            {
                await conn.OpenAsync(token);
                using (var writer = conn.BeginTextImport(GetCopyQuery(new CopyQueryParameters
                {
                    TableName = string.IsNullOrEmpty(schemaName) ? tableName : schemaName + "." + tableName,
                    Delimiter = ",",
                    Encoding = "UTF8",
                    Escape = '\"',
                    Format = "csv",
                    Header = false,
                    Null = "",
                    Quote = '\"'
                })))
                {
                    while (reader.Read())
                    {
                        await writer.WriteLineAsync(GetLine(reader));
                    }
                }
            }
        }

        private string GetLine(IDataReader reader)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (builder.Length > 0)
                    builder.Append(",");

                builder.Append("\"");
                builder.Append(reader.GetValue(i).ToString().Replace("\"","\"\""));
                builder.Append("\"");
            }
            
            return builder.ToString();
        }

        private string GetCopyQuery(CopyQueryParameters workerInput)
        {
            StringBuilder query = new StringBuilder("COPY ");
            query.Append(workerInput.TableName);
            query.Append(" FROM STDIN WITH (FORMAT ");
            query.Append(workerInput.Format ?? "text");
            if (workerInput.Oids.HasValue)
            {
                query.Append(", OIDS ");
                query.Append(workerInput.Oids.Value);
            }
            if (!string.IsNullOrEmpty(workerInput.Delimiter))
            {
                query.Append(", DELIMITER E'");
                query.Append(workerInput.Delimiter);
                query.Append("'");
            }
            if (!string.IsNullOrEmpty(workerInput.Null))
            {
                query.Append(", NULL '");
                query.Append(workerInput.Null);
                query.Append("'");
            }
            if (workerInput.Header.HasValue && workerInput.Format?.ToLower() == "csv")
            {
                query.Append(", HEADER ");
                query.Append(workerInput.Header.Value);
            }
            if (workerInput.Quote.HasValue)
            {
                query.Append(", QUOTE E'");
                query.Append(workerInput.Quote);
                query.Append("'");
            }
            if (workerInput.Escape.HasValue)
            {
                query.Append(", ESCAPE E'");
                query.Append(workerInput.Escape);
                query.Append("'");
            }
            if (workerInput.ForceQuote?.Length > 0)
            {
                query.Append(", FORCE_QUOTE (");
                query.Append(workerInput.ForceQuote.Aggregate((x,y) => x + "," + y));
                query.Append(")");
            }
            if (workerInput.ForceNotNull?.Length > 0)
            {
                query.Append(", FORCE_NOT_NULL (");
                query.Append(workerInput.ForceNotNull.Aggregate((x,y) => x + "," + y));
                query.Append(")");
            }

            if (!string.IsNullOrEmpty(workerInput.Encoding))
            {
                query.Append(", ENCODING '");
                query.Append(workerInput.Encoding);
                query.Append("'");
            }

            query.Append(")");
            
            return query.ToString();
        }
    }
}