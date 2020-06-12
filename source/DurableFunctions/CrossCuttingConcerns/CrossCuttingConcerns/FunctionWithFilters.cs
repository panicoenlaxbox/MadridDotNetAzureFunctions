using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CrossCuttingConcerns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#pragma warning disable 618

// https://github.com/Azure/azure-webjobs-sdk/wiki/Function-Filters
// https://github.com/Azure/azure-webjobs-sdk/issues/1284#issuecomment-499957159
// https://github.com/Azure/azure-webjobs-sdk/issues/1284#issuecomment-559191198

[assembly: FunctionsStartup(typeof(Startup))]

namespace CrossCuttingConcerns
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IFunctionFilter, LogFilter>();
            builder.Services.AddSingleton<IFunctionFilter, ErrorHandlingFilter>();
        }
    }
}

namespace CrossCuttingConcerns
{
    [ErrorHandler]
    public class FunctionWithFilters : IFunctionInvocationFilter, IFunctionExceptionFilter
    {
        //[ErrorHandler]
        [FunctionName("FunctionWithFilters")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //throw new DivideByZeroException();

            return new OkResult();
        }

        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            var logger = executingContext.Logger;
            logger.LogInformation($"Executing {executingContext.FunctionName} with instanceId {executingContext.FunctionInstanceId}");
            foreach (var (key, value) in executingContext.Arguments)
            {
                logger.LogInformation($"Parameter {key} with value {value}");
            }

            var req = (HttpRequest)executingContext.Arguments.Single(a => a.Value is HttpRequest).Value;
            req.HttpContext.User = new GenericPrincipal(new GenericIdentity("Sergio"), new string[] { });

            return Task.CompletedTask;
        }

        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            var logger = executedContext.Logger;
            logger.LogInformation($"Executed {executedContext.FunctionName} with instanceId {executedContext.FunctionInstanceId}");
            foreach (var (key, value) in executedContext.Arguments)
            {
                logger.LogInformation($"Parameter {key} with value {value}");
            }
            logger.LogInformation($"Result was {(executedContext.FunctionResult.Succeeded ? " succeeded" : " failed")}");
            if (executedContext.FunctionResult.Exception != null)
            {
                logger.LogInformation(executedContext.FunctionResult.Exception.ToString());
            }
            return Task.CompletedTask;
        }

        public Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            var logger = exceptionContext.Logger;
            logger.LogInformation($"Error in {exceptionContext.FunctionName} with instanceId {exceptionContext.FunctionInstanceId}");
            logger.LogInformation(exceptionContext.Exception.ToString());
            return Task.CompletedTask;
        }
    }

    public class LogFilter : IFunctionInvocationFilter
    {
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            executingContext.Logger.LogInformation($"Executing {nameof(OnExecutingAsync)} in filter {nameof(LogFilter)}");
            return Task.CompletedTask;
        }

        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            executedContext.Logger.LogInformation($"Executing {nameof(OnExecutedAsync)} in filter {nameof(LogFilter)}");
            return Task.CompletedTask;
        }
    }

    public class ErrorHandlingFilter : IFunctionExceptionFilter
    {
        public Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            exceptionContext.Logger.LogInformation($"Executing {nameof(OnExceptionAsync)} in filter {nameof(ErrorHandlingFilter)}");
            return Task.CompletedTask;
        }
    }

    public class ErrorHandlerAttribute : Attribute, IFunctionExceptionFilter
    {
        public Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            exceptionContext.Logger.LogInformation($"Executing {nameof(OnExceptionAsync)} in filter {nameof(ErrorHandlingFilter)}");
            return Task.CompletedTask;
        }
    }
}
