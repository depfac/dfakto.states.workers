using System;

namespace dFakto.States.Workers.Abstractions.Exceptions
{
    public class SqlConnectionException : WorkerException
    {
        public SqlConnectionException(Exception inner)
            : base("dFakto.SQL.ConnectionFailed", inner.Message, inner)
        {
            
        }
    }
}