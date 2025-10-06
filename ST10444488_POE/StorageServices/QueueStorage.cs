using Azure.Storage.Queues;
using System.Text.Json;

namespace ST10444488_POE.StorageServices
{
    public class QueueStorage
    {
        private readonly QueueClient _queueClient;

        public QueueStorage(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task SendMessageAsync<T>(T message)
        {
            string json = JsonSerializer.Serialize(message);
            await _queueClient.SendMessageAsync(json);
        }

    }
}
