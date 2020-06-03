using System;
using System.Runtime.Serialization;

namespace dFakto.States.Workers.Abstractions.Exceptions
{
   [Serializable]
   public class WorkerException : Exception
   {
      public string Error { get; }
      
      public WorkerException()
      {
      }

      public WorkerException(string error, string cause) : base(cause)
      {
         Error = error;
      }

      public WorkerException(string error, string cause, Exception inner) : base(cause, inner)
      {
         Error = error;
      }

      protected WorkerException(
         SerializationInfo info,
         StreamingContext context) : base(info, context)
      {
      }
   }
}