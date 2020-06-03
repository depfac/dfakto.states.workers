using System;

namespace dFakto.States.Workers.Abstractions.Exceptions
{
    public class SqlQueryException : WorkerException
    {
        public SqlQueryException(Exception inner)
            : base("dFakto.SQL.QueryFailed", inner.Message, inner)
        {
            
        }
    }
}