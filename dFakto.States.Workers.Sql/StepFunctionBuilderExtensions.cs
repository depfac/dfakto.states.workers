using System;
using System.Collections.Generic;
using dFakto.States.Workers.Sql.Common;
using dFakto.States.Workers.Sql.MySQL;
using dFakto.States.Workers.Sql.Oracle;
using dFakto.States.Workers.Sql.PostgreSQL;
using dFakto.States.Workers.Sql.SQLServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Sql
{
    public static class StepFunctionBuilderExtensions
    {
        public static StepFunctionsBuilder AddSqlWorkers(this StepFunctionsBuilder builder, IEnumerable<DatabaseConfig> databases)
        {
            if (databases != null)
            {
                foreach (var database in databases)
                {
                    switch (database.Type)
                    {
                        case SqlDatabaseType.SqlServer:
                            builder.ServiceCollection.AddSingleton<BaseDatabase>(x =>
                                new SqlServerBaseDatabase(database));
                            break;
                        case SqlDatabaseType.PostgreSql:
                            builder.ServiceCollection.AddSingleton<BaseDatabase>(x =>
                                new PostgreSqlBaseDatabase(database));
                            break;
                        case SqlDatabaseType.MariaDb:
                        case SqlDatabaseType.MySql:
                            builder.ServiceCollection.AddSingleton<BaseDatabase>(x => 
                                new MySqlDatabase(database));
                            break;
                        case SqlDatabaseType.Oracle:
                            builder.ServiceCollection.AddSingleton<BaseDatabase>(x => 
                                new OracleDatabase(database));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            builder.AddWorker<SqlQueryWorker>();
            builder.AddWorker<SqlBulkInsertWorker>();
            builder.AddWorker<SqlExportToCsvWorker>();
            return builder;
        }
    }
}