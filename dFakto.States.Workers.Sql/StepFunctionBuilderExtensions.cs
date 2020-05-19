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
            builder.ServiceCollection.AddTransient<SqlServerBaseDatabase>();
            builder.ServiceCollection.AddTransient<OracleDatabase>();
            builder.ServiceCollection.AddTransient<MySqlDatabase>();
            builder.ServiceCollection.AddTransient<PostgreSqlBaseDatabase>();
            
            if (databases != null)
            {
                foreach (var database in databases)
                {
                    try
                    {
                        builder.ServiceCollection.AddSingleton<BaseDatabase>(x =>
                        {
                            var t = Type.GetType(database.Type);
                            BaseDatabase db = (BaseDatabase) x.GetService(t);
                            db.Config = database;
                            return db;
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error while loading database '{database.Name}' type '{database.Type}'");
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