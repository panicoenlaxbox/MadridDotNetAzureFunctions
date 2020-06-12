using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsPatterns
{
    public static class Monitor
    {
        [FunctionName("Monitor_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var data = await req.Content.ReadAsAsync<dynamic>();
            var fileName = data.fileName;
            string instanceId = await starter.StartNewAsync("Monitor_Orchestrator", instanceId: null, input: fileName);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Monitor_Orchestrator")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var fileName = context.GetInput<string>();
            await context.CallActivityAsync("Monitor_StartLongTask", fileName);
            var timeoutAt = context.CurrentUtcDateTime.AddMinutes(30);
            while (true)
            {
                if (context.CurrentUtcDateTime > timeoutAt)
                {
                    context.SetCustomStatus("Timeout has been exceeded.");
                    break; // You must cancel long task if it's possible
                }
                if (await context.CallActivityAsync<bool>("Monitor_IsFinishedLongTask", fileName))
                {
                    context.SetCustomStatus("Long running task has finished.");
                    context.SetOutput($"http://somewhere.io/{fileName}");
                    break;
                }
                var fireAt = context.CurrentUtcDateTime.AddSeconds(15);
                await context.CreateTimer(fireAt, CancellationToken.None);
            }
        }

        [FunctionName("Monitor_StartLongTask")]
        public static void StartLongTask([ActivityTrigger] string fileName, ILogger log)
        {
            // Call API, start an async process, queue a message, etc.
            // This activity returns before the job is complete, its job is to just start the async/long running operation
            log.LogInformation($"{nameof(StartLongTask)} with {fileName}");
        }

        [FunctionName("Monitor_IsFinishedLongTask")]
        public static bool IsFinishedLongTask([ActivityTrigger] string fileName, ILogger log)
        {
            // Here you would make a call to an API, query a database, etc to check whether the long running async process is complete
            return new Random().Next() % 2 == 0;
        }
    }
}
