using Azure;
using Azure.Storage.Files.Shares;

namespace ST10444488_POE.StorageServices
{
    public class FileStorage
    {
        private readonly ShareDirectoryClient _directoryClient;

        public FileStorage(string connectionString, string shareName, string folderName)
        {
            var shareClient = new ShareClient(connectionString, shareName);
            shareClient.CreateIfNotExists();

            _directoryClient = shareClient.GetDirectoryClient(folderName);
            _directoryClient.CreateIfNotExists();
        }

        public async Task UploadFileAsync(string fileName, byte[] fileBytes)
        {
            var fileClient = _directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileBytes.Length);
            using var stream = new MemoryStream(fileBytes);
            await fileClient.UploadRangeAsync(new HttpRange(0, fileBytes.Length), stream);
        }
    }
}
