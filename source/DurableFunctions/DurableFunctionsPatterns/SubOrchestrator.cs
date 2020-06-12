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
    public static class SubOrchestrator
    {
        [FunctionName("SubOrchestrator_RunOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var value = await context.CallSubOrchestratorAsync<string>("SubOrchestrator_FunctionChaining", null);
            value = await context.CallSubOrchestratorAsync<string>("SubOrchestrator_FanOutFanIn", value);
            return value;
        }

        [FunctionName("SubOrchestrator_FunctionChaining")]
        public static async Task<string> FunctionChaining(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var result = await context.CallActivityAsync<string>("SubOrchestrator_Concat", ("Madrid", "Dot"));
            result = await context.CallActivityAsync<string>("SubOrchestrator_Concat", (result, "Net"));
            return result;
        }

        [FunctionName("SubOrchestrator_FanOutFanIn")]
        public static async Task<string> FanOutFanIn(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var input = context.GetInput<string>();
            var values = input.Split(",");
            var tasks = new List<Task<string>>();
            foreach (var value in values)
            {
                tasks.Add(context.CallActivityAsync<string>("SubOrchestrator_UpperCase", value));
            }
            await Task.WhenAll(tasks);
            return string.Join('_', tasks.Select(t => t.Result));
        }

        [FunctionName("SubOrchestrator_Concat")]
        public static string Concat([ActivityTrigger] (string value, string value2) values, ILogger log)
        {
            var (value, value2) = values;
            return $"{value},{value2}";
        }

        [FunctionName("SubOrchestrator_UpperCase")]
        public static string UpperCase([ActivityTrigger] string value, ILogger log)
        {
            return value.ToUpper();
        }

        [FunctionName("SubOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var instanceId = await starter.StartNewAsync("SubOrchestrator_RunOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}