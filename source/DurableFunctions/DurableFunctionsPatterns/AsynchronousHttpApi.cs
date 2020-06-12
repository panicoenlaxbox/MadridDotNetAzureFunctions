using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsPatterns
{
    public static class AsynchronousHttpApi
    {
        [FunctionName("AsynchronousHttpApi_RunOrchestrator")]
        public static async Task<int> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var tasks = new List<Task<int>>();

            for (var i = 0; i < 10; i++)
            {
                tasks.Add(context.CallActivityAsync<int>("AsynchronousHttpApi_RunActivity", i));
            }

            await Task.WhenAll(tasks);

            return tasks.Sum(t => t.Result);
        }


        [FunctionName("AsynchronousHttpApi_RunActivity")]
        public static async Task<int> RunActivity([ActivityTrigger] int seconds, ILogger log)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return seconds;
        }

        [FunctionName("AsynchronousHttpApi_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var instanceId = await starter.StartNewAsync("AsynchronousHttpApi_RunOrchestrator", instanceId: null);

            return new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Accepted,
                Content = new StringContent(
                    $"{req.RequestUri.Scheme}://{req.RequestUri.Host}:{(!req.RequestUri.IsDefaultPort ? req.RequestUri.Port.ToString() : "")}/api/status?id={instanceId}"),
            };
        }

        [FunctionName("AsynchronousHttpApi_Status")]
        public static async Task<IActionResult> Status(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableOrchestrationClient,
            ILogger log)
        {
            var instanceId = req.Query["id"];

            if (string.IsNullOrWhiteSpace(instanceId))
                return new NotFoundResult();

            var status = await durableOrchestrationClient.GetStatusAsync(instanceId);

            if (status is null)
                return new NotFoundResult();

            return new OkObjectResult(new
            {
                status = status.RuntimeStatus.ToString(),
                result = status.Output
            });
        }
    }
}
