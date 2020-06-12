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
    public static class Timeout
    {
        [FunctionName("Timeout_RunOrchestrator")]
        public static async Task<bool> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var timeoutAt = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(10));

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Don't wait to task
                var activityTask = context.CallActivityAsync("Timeout_SayHello", "Sergio");
                var timeoutTask = context.CreateTimer(timeoutAt, cancellationTokenSource.Token);

                // When any finish
                var winner = await Task.WhenAny(activityTask, timeoutTask);
                if (winner == activityTask)
                {
                    // SayHello case
                    cancellationTokenSource.Cancel(); // Cancel timer
                    return true;
                }
                // Timeout case
                return false;
            }
        }

        [FunctionName("Timeout_SayHello")]
        public static async Task<string> SayHello([ActivityTrigger] string name)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return $"Hi {name}";
        }

        [FunctionName("Timeout_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var instanceId = await starter.StartNewAsync("Timeout_RunOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
