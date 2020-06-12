using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// local.settings.json
/*
    "Queue": "myqueue-items2",
    "BlobContainer_Raw": "samples-workitems/raw",
    "BlobContainer_Processed": "samples-workitems/processed",
    "CustomersTable":  "Customers" 
*/

// PowerShell Core
// curl -H 'Content-Type: application/json' -d '{ ""Id"": 1, ""Name"": ""ACME"" }' http://localhost:7071/api/CreateCustomer/es
namespace Bindings
{
    public static class CreateCustomerFunction
    {
        [FunctionName(nameof(CreateCustomerFunction))]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CreateCustomer/{country}")] Customer customer,
            string country,
            [Blob("%BlobContainer_Raw%/{rand-guid}.{country}", FileAccess.Write)] out string blob,
            ILogger log)
        {
            blob = JsonConvert.SerializeObject(customer);
        }
    }

    public class BlobInfo
    {
        public string Container { get; set; }
        public string FileName { get; set; }
        public string Country { get; set; }
    }

    public static class RenameCustomerFunction
    {
        [FunctionName(nameof(RenameCustomerFunction))]
        [return: Queue("%Queue%")]
        public static async Task<BlobInfo> Run([BlobTrigger("%BlobContainer_Raw%/{id}.{country}")] CloudBlockBlob blob,
            string country, Binder binder, ILogger log)
        {
            var text = await blob.DownloadTextAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(text);
            var newFilename = $"{customer.Id}__{customer.Name.Replace(" ", "_")}__{country.ToLower()}.txt";
            var path = $"%BlobContainer_Processed%/{newFilename}";
            var attributes = new Attribute[]
            {
                new BlobAttribute(path,FileAccess.Write)
            };
            using (var writer = await binder.BindAsync<TextWriter>(attributes))
            {
                await writer.WriteAsync(text);
            }
            var blobInfo = new BlobInfo()
            {
                Container = Environment.GetEnvironmentVariable("BlobContainer_Processed"),
                FileName = newFilename,
                Country = country
            };
            await blob.DeleteAsync();
            return blobInfo;
        }
    }

    public class CustomerEntity : TableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string Path { get; set; }
    }

    public static class WriteCustomerFunction
    {
        [FunctionName(nameof(WriteCustomerFunction))]
        [return: Table("%CustomersTable%")]
        public static CustomerEntity Run([QueueTrigger("%Queue%")] BlobInfo blobInfo,
            [Blob("{Container}/{FileName}", FileAccess.Read)] string blob,
            ILogger log)
        {
            var customer = JsonConvert.DeserializeObject<Customer>(blob);
            return new CustomerEntity()
            {
                PartitionKey = blobInfo.Country,
                RowKey = customer.Id.ToString(),
                Id = customer.Id,
                Name = customer.Name,
                Country = blobInfo.Country,
                Path = $"{blobInfo.Container}/{blobInfo.FileName}"
            };
        }
    }
}
