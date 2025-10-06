using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ST10444488_POE.Models;

namespace Ginger_Bakery_Functions
{
    public class TableStorageFunction
    {
        [Function("SaveProductToTable")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SaveProductToTable");
            logger.LogInformation("Received request to save product.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Product product;

            try
            {
                product = JsonSerializer.Deserialize<Product>(requestBody);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize product.");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Invalid JSON format." });
                return errorResponse;
            }

            if (product == null || string.IsNullOrEmpty(product.RowKey) ||
                string.IsNullOrEmpty(product.Name) || product.Price <= 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "RowKey, Name, and valid Price are required." });
                return badResponse;
            }

            var connectionString = Environment.GetEnvironmentVariable("connection");
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
                message = "Product saved successfully.",
                productId = product.RowKey
            });

            return response;
        }
    }
}