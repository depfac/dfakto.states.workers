using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Internals
{
    public interface IHeartbeatManager
    {
        void RegisterHeartbeat(TimeSpan delay, string taskToken, CancellationTokenSource token);
        void UnregisterHeartbeat(string taskToken);
    }
    
    internal class HeartbeatHostedService : CancellableHostedService, IHeartbeatManager
    {
        private class HeartbeatTask
        {
            public DateTime NextHeartBeat { get; set; }
            public TimeSpan Delay { get; set; }
            public string TaskToken { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
        
        private readonly AmazonStepFunctionsClient _client;
        private readonly ILogger<HeartbeatHostedService> _logger;
        private readonly List<HeartbeatTask> _tasks = new List<HeartbeatTask>();

        public HeartbeatHostedService(
            AmazonStepFunctionsClient client,
            ILogger<HeartbeatHostedService> logger)
        {
            logger.LogDebug($"Creating HeartbeatHostedService");
            
            _client = client;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            _logger.LogInformation($"Heartbeat service started");

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token);

                if (_tasks.Count == 0)
                    continue;

                DateTime now = DateTime.Now;

                try
                {
                    List<HeartbeatTask> tasks = new List<HeartbeatTask>();
                    lock (_tasks)
                    {
                        while (_tasks.Count > 0 && _tasks[0].NextHeartBeat < now)
                        {
                            tasks.Add(_tasks[0]);
                            _tasks.RemoveAt(0);
                        }
                    }

                    foreach (var t in tasks)
                    {
                        _logger.LogDebug($"Sending Heartbeat for token '{t.TaskToken}'");
                        var r = await _client.SendTaskHeartbeatAsync(new SendTaskHeartbeatRequest
                        {
                            TaskToken = t.TaskToken
                        }, token);

                        if (r.HttpStatusCode != HttpStatusCode.OK)
                        {
                            _logger.LogDebug($"Heartbeat received an error '{t.TaskToken}', cancelling worker");
                            t.CancellationTokenSource.Cancel();
                        }
                        else
                        {
                            RegisterHeartbeat(t.Delay,t.TaskToken,t.CancellationTokenSource);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error while sending heartbeat");
                }
            }

            _logger.LogDebug($"Heartbeat service stopped");
        }

        public void RegisterHeartbeat(TimeSpan delay, string taskToken, CancellationTokenSource token)
        {
            if (delay != TimeSpan.MaxValue)
            {
                lock (_tasks)
                {
                    HeartbeatTask task = new HeartbeatTask
                    {
                        Delay = delay,
                        TaskToken = taskToken,
                        CancellationTokenSource = token,
                        NextHeartBeat = DateTime.Now.Add(delay)
                    };

                    int index = 0;
                    foreach (var t in _tasks)
                    {
                        if (t.NextHeartBeat > task.NextHeartBeat)
                            break;
                        index++;
                    }

                    _tasks.Insert(index, task);
                }
            }
        }

        public void UnregisterHeartbeat(string taskToken)
        {
            lock (_tasks)
            {
                int index = 0;
                foreach (var t in _tasks)
                {
                    if(t.TaskToken == taskToken)
                        break;
                    index++;
                }

                if (index < _tasks.Count)
                {
                    _tasks.RemoveAt(index);
                }
            }
        }
    }
}