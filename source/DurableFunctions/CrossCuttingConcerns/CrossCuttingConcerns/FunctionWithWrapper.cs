using System;
using System.Reflection;
using System.Threading.Tasks;
using CrossCuttingConcerns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup2))]
namespace CrossCuttingConcerns
{
    public class Startup2 : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddOptions<MyConfiguration>()
                .Configure<IConfiguration>((myConfiguration, configuration) =>
                {
                    configuration.Bind(myConfiguration);
                });

            builder.Services.AddTransient<IFunctionWrapper, FunctionWrapper>();

            // https://github.com/Azure/azure-functions-dotnet-extensions/issues/17#issuecomment-499086297
            var executionContextOptions = builder.Services.BuildServiceProvider()
                .GetService<IOptions<ExecutionContextOptions>>().Value;

            var functionEnvironment = new FunctionEnvironment()
            {
                WebSiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME"),
                EnvironmentName = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"),
                AppDirectory = executionContextOptions.AppDirectory
            };

            functionEnvironment.EnsureHasEnvironment();

            builder.Services.AddSingleton(functionEnvironment);

            functionEnvironment.IfDevelopment(() =>
            {
                // do something...
            });

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(executionContextOptions.AppDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{functionEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

            var configuration = configurationBuilder.Build();
            builder.Services.Configure<MyOtherConfiguration>(configuration.GetSection("myOtherConfiguration"));
        }
    }
}

namespace CrossCuttingConcerns
{
    public class FunctionWithWrapper
    {
        private readonly IFunctionWrapper _wrapper;
        private readonly FunctionEnvironment _functionEnvironment;
        private readonly MyConfiguration _myConfiguration;
        private readonly MyOtherConfiguration _myOtherConfiguration;

        public FunctionWithWrapper(IFunctionWrapper wrapper, FunctionEnvironment functionEnvironment, IOptions<MyConfiguration> myConfiguration, IOptions<MyOtherConfiguration> myOtherConfiguration)
        {
            _wrapper = wrapper;
            _functionEnvironment = functionEnvironment;
            _myConfiguration = myConfiguration.Value;
            _myOtherConfiguration = myOtherConfiguration.Value;
        }

        [FunctionName("FunctionWithWrapper")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] Customer customer, HttpRequest req,
            ILogger log, ExecutionContext executionContext)
        {
            return await _wrapper.Execute(customer, req, async () =>
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                //throw new DivideByZeroException();

                return new OkResult();
            });
        }
    }

    public interface IFunctionWrapper
    {
        Task<IActionResult> Execute<T>(T model, HttpRequest req, Func<Task<IActionResult>> function);
    }

    public class FunctionWrapper : IFunctionWrapper
    {
        private readonly ILogger<FunctionWrapper> _log;

        public FunctionWrapper(ILogger<FunctionWrapper> log)
        {
            _log = log;
        }

        public async Task<IActionResult> Execute<T>(T model, HttpRequest req, Func<Task<IActionResult>> function)
        {
            try
            {
                // Do something...
                return await function();
            }
            catch (Exception e)
            {
                _log.LogError($"Unhandled exception {e.Message}");
                return new ObjectResult(e.Message)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
