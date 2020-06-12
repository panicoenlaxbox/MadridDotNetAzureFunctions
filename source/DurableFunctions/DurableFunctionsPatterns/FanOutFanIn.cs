using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsPatterns
{
    public static class FanOutFanIn
    {
        [FunctionName("FanOutFanIn_RunOrchestrator")]
        public static async Task<int> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var tasks = new List<Task<int>>();

            for (var i = 0; i < 10; i++)
            {
                tasks.Add(context.CallActivityAsync<int>("FanOutFanIn_RunActivity", i));
            }

            await Task.WhenAll(tasks);

            return tasks.Sum(t => t.Result);
        }


        [FunctionName("FanOutFanIn_RunActivity")]
        public static async Task<int> RunActivity([ActivityTrigger] int seconds, ILogger log)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return seconds;
        }

        [FunctionName("FanOutFanIn_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("FanOutFanIn", instanceId: null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}