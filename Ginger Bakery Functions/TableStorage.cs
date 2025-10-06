using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ST10444488_POE.Models;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ginger_Bakery_Functions
{
    public class TableStorage
    {
        [Function("SaveProductToTable")]
        public async Task<HttpResponseData> SaveProductToTable(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("SaveProductToTable");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var product = JsonSerializer.Deserialize<Product>(requestBody);

            if (product == null || string.IsNullOrEmpty(product.RowKey) ||
                string.IsNullOrEmpty(product.Name) || product.Price <= 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "RowKey, Name, and valid Price are required." });
                return badResponse;
            }

            var connectionString = Environment.GetEnvironmentVariable("StorageConnection");
            var tableClient = new TableClient(connectionString, "Products");
            await tableClient.CreateIfNotExistsAsync();

            product.PartitionKey ??= "Product";
            product.Timestamp = DateTimeOffset.UtcNow;
            product.ETag = ETag.All;

            await tableClient.UpsertEntityAsync(product);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Product saved successfully",
                productId = product.RowKey
            });
            return response;
        }
    }
}
