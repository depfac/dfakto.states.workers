using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Abstractions.Exceptions;
using dFakto.States.Workers.Config;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Internals
{
    public class WorkerHostedService : CancellableHostedService
    {
        private readonly IHeartbeatManager _heartbeatManager;
        private readonly ILogger<WorkerHostedService> _logger;
        private long _runningTasks = 0;

        public WorkerHostedService(IWorker worker,
            IHeartbeatManager heartbeatManager,
            StepFunctionsConfig config,
            AmazonStepFunctionsClient client,
            ILoggerFactory loggerFactory)
        {
            _heartbeatManager = heartbeatManager;
            _logger = loggerFactory.CreateLogger<WorkerHostedService>();
            LoggerFactory = loggerFactory;
            Client = client;
            Worker = worker;
            Config = config;

            _logger.LogDebug($"Creating TaskWorkerHostedService: '{worker.ActivityName}'");
        }

        public AmazonStepFunctionsClient Client { get; }
        public IWorker Worker { get; }
        public StepFunctionsConfig Config { get; }
        public ILoggerFactory LoggerFactory { get; set; }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            _logger.LogInformation($"Starting '{Worker.ActivityName}' Worker");
            
            var activityName =
                !string.IsNullOrEmpty(Config.EnvironmentName)
                    ? Config.EnvironmentName + "-" + Worker.ActivityName
                    : Worker.ActivityName;
            
            while (!token.IsCancellationRequested)
            {
                var activityArn = string.Empty;
                while (activityArn == string.Empty && !token.IsCancellationRequested)
                {
                    try
                    {
                        // 1) Register activity
                        _logger.LogDebug($"Registering Activity '{activityName}'");
                        var activity = await Client.CreateActivityAsync(new CreateActivityRequest {Name = activityName}, token);
                        activityArn = activity.ActivityArn;
                        _logger.LogDebug($"Activity '{activityName}' Registered (ARN: '{activity.ActivityArn}')");
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("Unable to register worker :"+e.Message);
                        Thread.Sleep(Config.RegisterRetryDelay * 1000);
                    }
                }

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // 2) Wait for available task slot
                        while (_runningTasks >= Worker.MaxConcurrency && !token.IsCancellationRequested)
                        {
                            _logger.LogDebug($"Waiting for an empty slot for '{activityArn}'");
                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                        }

                        // 3) Wait for a task
                        var workerName = GetWorkerName(activityName);

                        _logger.LogDebug($"Waiting for a job on activity '{activityArn}' ({workerName})");
                        var activityTask = await Client.GetActivityTaskAsync(new GetActivityTaskRequest
                            {
                                ActivityArn = activityArn,
                                WorkerName = workerName
                            },
                            token);

                        if (string.IsNullOrEmpty(activityTask.TaskToken))
                        {
                            _logger.LogDebug("No TaskToken, let's wait again");
                            continue;
                        }

                        Interlocked.Increment(ref _runningTasks);

                        var heartBeatCancellationToken = new CancellationTokenSource();
                        // 4) Register heartbeat
                        _heartbeatManager.RegisterHeartbeat(Worker.HeartbeatDelay, activityTask.TaskToken, heartBeatCancellationToken);

                        // 5) Execute worker in a separate thread
                        var r = Task.Run(async () =>
                        {
                            _logger.LogDebug(
                                $"Start processing activity '{activityArn}' ('{activityTask.TaskToken}')");

                            var workerCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, heartBeatCancellationToken.Token);
                            // 6) Call worker 
                            await Worker.DoRawJsonWorkAsync(activityTask.Input, workerCancellationToken.Token)
                                .ContinueWith(x =>
                                    TaskCompleted(
                                        x,
                                        activityTask.TaskToken));
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("Task Cancelled, stopping");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error while processing activity '{activityName}'.");
                }
            }

            _logger.LogDebug($"Listening on Activity '{activityName}' stopped");
        }

        private string GetWorkerName(string activityName)
        {
            string workerName = $"{Environment.MachineName}-{activityName}-{_runningTasks:0000}";
            if (workerName.Length > 80)
            {
                workerName = workerName.Substring(workerName.Length - 80);
            }

            return workerName;
        }

        private async Task TaskCompleted(Task<string> output, string taskToken)
        {
            // 7) Unregister Heartbeat
            _heartbeatManager.UnregisterHeartbeat(taskToken);
            Interlocked.Decrement(ref _runningTasks);

            if (output.IsCompleted && !output.IsCanceled && !output.IsFaulted)
            {
                //8) Send success
                _logger.LogInformation($"Sending Task Succeed for token '{taskToken}'");
                await Client.SendTaskSuccessAsync(new SendTaskSuccessRequest
                {
                    TaskToken = taskToken,
                    Output = output.Result
                });
            }
            else
            {
                //8) Send failure
                var workerException = (WorkerException) output.Exception?.InnerExceptions.FirstOrDefault(x => x is WorkerException);
                
                _logger.LogError(output.Exception, $"Sending Task Failed for token '{taskToken}'");
                
                await Client.SendTaskFailureAsync(new SendTaskFailureRequest
                {
                    TaskToken = taskToken,
                    Error = workerException != null ? workerException.Error : "dFakto.Worker.UnhandledError",
                    Cause = workerException != null ? workerException.Message : output.Exception?.Message
                });
            }
        }
    }
}