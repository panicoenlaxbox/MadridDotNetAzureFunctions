using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;


// http://localhost:7071/api/Function1/1
// http://localhost:7071/my_proxy/1?age=44
// http://localhost:7071/info
namespace Proxies
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Function1/{id?}")] HttpRequest req,
            int? id,
            ExecutionContext executionContext,
            ILogger log)
        {
            foreach ((string key, StringValues value) in req.Headers)
            {
                log.LogInformation($"Header {key}, {string.Join(',', value.ToArray())}");
            }

            foreach ((string key, StringValues value) in req.Query)
            {
                log.LogInformation($"Query {key}, {string.Join(',', value.ToArray())}");
            }

            if (req.HasFormContentType)
            {
                // application/x-www-form-urlencoded
                // multipart/form-data
                foreach ((string key, StringValues value) in req.Form)
                {
                    log.LogInformation($"Form {key}, {string.Join(',', value.ToArray())}");
                }
            }
            else
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"Body {requestBody}");
            }

            log.LogInformation($"{nameof(executionContext.FunctionAppDirectory)}, {executionContext.FunctionAppDirectory}");
            log.LogInformation($"{nameof(executionContext.FunctionDirectory)}, {executionContext.FunctionDirectory}");
            log.LogInformation($"{nameof(Directory.GetCurrentDirectory)}, {Directory.GetCurrentDirectory()}");

            return new OkObjectResult("Ok");
        }
    }
}
