using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using Azure;


namespace Ginger_Bakery_Functions
{
    public static class FileUpload
    {
        [Function("UploadCustomerFile")]
        public static async Task<HttpResponseData> UploadCustomerFile(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)

        {
            var logger = executionContext.GetLogger("UploadCustomerFile");
            logger.LogInformation("Received file upload request");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var uploadRequest = JsonSerializer.Deserialize<FileUploadRequest>(requestBody);

                if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.CustomerId) ||
                    string.IsNullOrEmpty(uploadRequest.FileName) || string.IsNullOrEmpty(uploadRequest.FileData))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "CustomerId, FileName, and FileData are required." });
                    return badResponse;
                }

                var connectionString = "DefaultEndpointsProtocol=https;AccountName=st10444488storage;AccountKey=A8CZd4IDN6EjwcyWTiQCEHIQyfizs+w7se4OpZzM8II/4i/pYSWvS4t/NPzwyW+lak8HG1SBWH9a+AStn44BLw==;EndpointSuffix=core.windows.net";
                var shareClient = new ShareClient(connectionString, "customer-files");
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetDirectoryClient("documents");
                await directoryClient.CreateIfNotExistsAsync();

                string uniqueFileName = $"{uploadRequest.CustomerId}_{Guid.NewGuid():N}{Path.GetExtension(uploadRequest.FileName)}";
                var fileClient = directoryClient.GetFileClient(uniqueFileName);

                byte[] fileBytes = Convert.FromBase64String(uploadRequest.FileData);
                await fileClient.CreateAsync(fileBytes.Length);
                using var stream = new MemoryStream(fileBytes);
                await fileClient.UploadRangeAsync(new HttpRange(0, fileBytes.Length), stream);

                logger.LogInformation($"File uploaded: {uniqueFileName}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    fileName = uniqueFileName,
                    message = "File uploaded successfully"
                });
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in UploadCustomerFile function");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }
    }

    public class FileUploadRequest
    {
        public string CustomerId { get; set; }
        public string FileName { get; set; }
        public string FileData { get; set; } // base64-encoded file
    }
}
