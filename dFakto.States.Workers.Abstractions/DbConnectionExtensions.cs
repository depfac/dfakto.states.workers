using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace dFakto.States.Workers.Sql.Common
{
    public static class DbConnectionExtensions
    {
        public static DbCommand CreateCommand(this DbConnection connection, Abstractions.SqlQuery input)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = input.Query;

            if (input.Params != null)
            {
                foreach (var p in input.Params)
                {
                    var para = cmd.CreateParameter();
                    para.ParameterName = p.Name;
                    switch (p.Value.ValueKind)
                    {
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Null:
                            para.Value = null;
                            break;
                        case JsonValueKind.String:
                            para.Value = p.Value.GetString();
                            para.DbType = DbType.String;
                            break;
                        case JsonValueKind.Number:
                            para.Value = p.Value.GetDecimal();
                            para.DbType = DbType.Decimal;
                            break;
                        case JsonValueKind.True:
                            para.Value = true;
                            para.DbType = DbType.Boolean;
                            break;
                        case JsonValueKind.False:                            
                            para.Value = false;
                            para.DbType = DbType.Boolean;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                        
                    cmd.Parameters.Add(para);
                }
            }
            return cmd;
        }
    }
}