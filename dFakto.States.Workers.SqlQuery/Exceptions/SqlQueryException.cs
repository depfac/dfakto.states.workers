using System;
using dFakto.States.Workers.Abstractions;

namespace dFakto.States.Workers.Sql.Exceptions
{
    public class SqlQueryException : WorkerException
    {
        public SqlQueryException(Exception inner)
            : base("dFakto.SQL.QueryFailed", inner.Message, inner)
        {
            
        }
    }
}