using System;
using System.Threading;
using System.Threading.Tasks;

namespace dFakto.States.Workers.Abstractions
{
    public interface IWorker
    {
        string ActivityName { get; }
        TimeSpan HeartbeatDelay { get; }
        int MaxConcurrency { get; }
        Task<string> DoRawJsonWorkAsync(string input, CancellationToken token);
    }
}