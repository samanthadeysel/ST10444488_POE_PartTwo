using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ginger_Bakery_Functions
{
    public class QueueTrigger
    {
        [Function("ProcessOrderQueue")]
        public async Task ProcessOrderQueue(
            [QueueTrigger("order-queue", Connection = "StorageConnection")] string message,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProcessOrderQueue");
            logger.LogInformation($"Processing order message: {message}");

            try
            {
                var order = JsonSerializer.Deserialize<OrderMessage>(message);
                // Add your order processing logic here
                logger.LogInformation($"Order for {order.CustomerName} processed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process order message.");
            }
        }
    }

    public class OrderMessage
    {
        public string CustomerName { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }

}

