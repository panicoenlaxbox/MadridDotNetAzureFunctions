using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsPatterns
{
    public static class EternalOrchestration
    {
        //private static DateTime _beginAt; // don't do this in production

        [FunctionName("EternalOrchestration_RunOrchestrator")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await context.CallActivityAsync("EternalOrchestration_SayHello", "Sergio");

            var fireAt = context.CurrentUtcDateTime.AddSeconds(5);

            await context.CreateTimer(fireAt, CancellationToken.None);

            context.ContinueAsNew(null);

            //if ((context.CurrentUtcDateTime - _beginAt).TotalSeconds < 60)
            //{
            //    context.ContinueAsNew(null);
            //}
            //else
            //{
            //    context.SetOutput("The end.");
            //}
        }

        [FunctionName("EternalOrchestration_SayHello")]
        public static string SayHello([ActivityTrigger] string name)
        {
            return $"Hi {name}";
        }

        [FunctionName("EternalOrchestration_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            //_beginAt = DateTime.UtcNow;

            string instanceId = await starter.StartNewAsync("EternalOrchestration_RunOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}