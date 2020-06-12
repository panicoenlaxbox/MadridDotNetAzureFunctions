using System;
using System.Threading;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bindings
{
    // Microsoft.Azure.WebJobs.Extensions.Storage
    // { Id: 1, Name: "ACME" }

    //public static class QueueTrigger
    //{
    //    [FunctionName("QueueTrigger")]
    //    public static void Run([QueueTrigger("myqueue-items")] CloudQueueMessage message, ILogger log)
    //    {
    //        log.LogInformation($"{nameof(message.InsertionTime)} {message.InsertionTime:g}");
    //        log.LogInformation(JsonConvert.DeserializeObject<Customer>(message.AsString).ToString());
    //    }
    //}


    //public static class QueueTrigger
    //{
    //    [FunctionName("QueueTrigger")]
    //    public static void Run([QueueTrigger("myqueue-items")] Customer customer, ILogger log)
    //    {
    //        log.LogInformation(customer.ToString());
    //    }
    //}

    //public static class QueueTrigger
    //{
    //    [FunctionName("QueueTrigger")]
    //    public static void Run([QueueTrigger("myqueue-items")] Customer customer, ILogger log,
    //        string queueTrigger,
    //        int dequeueCount,
    //        DateTimeOffset expirationTime,
    //        string id,
    //        DateTimeOffset insertionTime,
    //        DateTimeOffset nextVisibleTime,
    //        string popReceipt)
    //    {
    //        log.LogInformation(customer.ToString());
    //    }
    //}

    public static class QueueTrigger
    {
        [FunctionName("QueueTrigger")]
        public static void Run([QueueTrigger("myqueue-items")] Customer customer, ILogger log,
            string queueTrigger,
            int dequeueCount,
            DateTimeOffset expirationTime,
            string id,
            DateTimeOffset insertionTime,
            DateTimeOffset nextVisibleTime,
            string popReceipt,
            Microsoft.Azure.WebJobs.ExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            log.LogInformation(customer.ToString());
        }
    }
}