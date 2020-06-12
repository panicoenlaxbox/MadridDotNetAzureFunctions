using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsPatterns
{
    public static class AsynchronousHumanInteraction
    {
        [FunctionName("AsynchronousHumanInteraction_Challenge")]
        public static async Task<bool> Challenge(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var verificationCode = await context.CallActivityAsync<int>("AsynchronousHumanInteraction_SendVerificationCode", "12345");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var expirationTime = context.CurrentUtcDateTime.AddSeconds(180);

                Task timeout = context.CreateTimer(expirationTime, cancellationTokenSource.Token);

                var authorized = false;

                Task<int> responseTask = context.WaitForExternalEvent<int>("verificationCodeSended");

                Task winner = await Task.WhenAny(responseTask, timeout);

                if (winner == responseTask && responseTask.Result == verificationCode)
                {
                    authorized = true;
                }

                if (!timeout.IsCompleted)
                {
                    cancellationTokenSource.Cancel();
                }

                return authorized;
            }
        }

        [FunctionName("AsynchronousHumanInteraction_SendVerificationCode")]
        public static int SendVerificationCode([ActivityTrigger] string phoneNumber, string instanceId, ILogger log)
        {
            var verificationCode = new Random(Guid.NewGuid().GetHashCode()).Next(10000);
            log.LogInformation($"instanceId {instanceId}, verificationCode {verificationCode}");
            return verificationCode;
        }

        [FunctionName("AsynchronousHumanInteraction_ChallengeResponse")]
        public static async Task ChallengeResponse(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client, ILogger log)
        {
            var instanceId = req.Query["instanceId"];
            var verificationCode = int.Parse(req.Query["verificationCode"]);
            log.LogInformation($"Sending response with verificationCode {verificationCode}");
            await client.RaiseEventAsync(instanceId, "verificationCodeSended", verificationCode);
        }

        [FunctionName("AsynchronousHumanInteraction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var instanceId = await starter.StartNewAsync("AsynchronousHumanInteraction_Challenge", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
