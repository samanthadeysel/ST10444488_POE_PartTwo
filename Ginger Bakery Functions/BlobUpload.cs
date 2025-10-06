using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ginger_Bakery_Functions
{
    public class BlobUpload
    {
        [Function("UploadProductImage")]
        public async Task<HttpResponseData> UploadProductImage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadProductImage");
            logger.LogInformation("HTTP trigger function received image upload request");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var uploadRequest = JsonSerializer.Deserialize<ImageUploadRequest>(requestBody);

                if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.FileName) ||
                    string.IsNullOrEmpty(uploadRequest.ProductId) || string.IsNullOrEmpty(uploadRequest.FileData))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "FileName, ProductId and FileData are required" });
                    return badResponse;
                }

                var connectionString = "DefaultEndpointsProtocol=https;AccountName=st10444488storage;AccountKey=A8CZd4IDN6EjwcyWTiQCEHIQyfizs+w7se4OpZzM8II/4i/pYSWvS4t/NPzwyW+lak8HG1SBWH9a+AStn44BLw==;EndpointSuffix=core.windows.net";
                var containerClient = new BlobContainerClient(connectionString, "product-images");
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                string uniqueFileName = $"{uploadRequest.ProductId}_{Guid.NewGuid():N}{Path.GetExtension(uploadRequest.FileName)}";
                byte[] fileBytes = Convert.FromBase64String(uploadRequest.FileData);
                using var fileStream = new MemoryStream(fileBytes);

                var blobClient = containerClient.GetBlobClient(uniqueFileName);
                await blobClient.UploadAsync(fileStream, overwrite: true);

                logger.LogInformation($"Image uploaded via function: {uniqueFileName}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    imageUrl = blobClient.Uri.ToString(),
                    fileName = uniqueFileName
                });
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }

        [Function("DeleteProductImage")]
        public async Task<HttpResponseData> DeleteProductImage(
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("DeleteProductImage");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var deleteRequest = JsonSerializer.Deserialize<DeleteImageRequest>(requestBody);

                if (deleteRequest == null || string.IsNullOrEmpty(deleteRequest.ImageUrl))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "ImageUrl is required" });
                    return badResponse;
                }

                var connectionString = "DefaultEndpointsProtocol=https;AccountName=st10444488storage;AccountKey=A8CZd4IDN6EjwcyWTiQCEHIQyfizs+w7se4OpZzM8II/4i/pYSWvS4t/NPzwyW+lak8HG1SBWH9a+AStn44BLw==;EndpointSuffix=core.windows.net";
                var uri = new Uri(deleteRequest.ImageUrl);
                var containerClient = new BlobContainerClient(connectionString, "product-images");
                var blobName = uri.Segments[^1];

                var blobClient = containerClient.GetBlobClient(blobName);
                bool deleted = await blobClient.DeleteIfExistsAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    deleted = deleted,
                    message = deleted ? "Image deleted successfully" : "Image not found"
                });
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DeleteProductImage function");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }
    }
    public class ImageUploadRequest
    {
        public string ProductId { get; set; }
        public string FileName { get; set; }
        public string FileData { get; set; }
    }

    public class DeleteImageRequest
    {
        public string ImageUrl { get; set; }
    }
}
