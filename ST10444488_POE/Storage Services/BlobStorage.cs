using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ST10444488_POE.StorageServices
{
    public class BlobStorage
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorage(string connectionString, string containerName)
        {
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadImageAsync(string fileName, string base64Data)
        {
            string uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            byte[] bytes = Convert.FromBase64String(base64Data);
            using var stream = new MemoryStream(bytes);

            var blobClient = _containerClient.GetBlobClient(uniqueName);
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            var blobName = new Uri(imageUrl).Segments[^1];
            var blobClient = _containerClient.GetBlobClient(blobName);
            var result = await blobClient.DeleteIfExistsAsync();
            return result;
        }
    }
}
