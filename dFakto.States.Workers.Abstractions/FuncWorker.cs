using System;
using System.Threading;
using System.Threading.Tasks;

namespace dFakto.States.Workers.Abstractions
{
    public class FuncWorker : IWorker
    {
        private readonly Func<string, Task<string>> _func;

        public FuncWorker(string name, Func<string, Task<string>> func, int maxConcurrency = 1)
        {
            _func = func;
            ActivityName = name;
            MaxConcurrency = maxConcurrency;
        }

        public string ActivityName { get; }
        public TimeSpan HeartbeatDelay => TimeSpan.MaxValue;
        public int MaxConcurrency { get; }

        public async Task<string> DoRawJsonWorkAsync(string input, CancellationToken token)
        {
            return await _func(input);
        }
    }
}