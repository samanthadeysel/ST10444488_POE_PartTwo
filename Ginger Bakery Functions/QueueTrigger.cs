using System;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ginger_Bakery_Functions
{
    public class OrderQueueFunction
    {
        [Function("ProcessOrderQueue")]
        public void Run(
            [QueueTrigger("order-queue", Connection = "connection")] string queueMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProcessOrderQueue");
            logger.LogInformation($"Received queue message: {queueMessage}");

            try
            {
                var order = JsonSerializer.Deserialize<OrderMessage>(queueMessage);

                if (order == null || string.IsNullOrEmpty(order.CustomerName))
                {
                    logger.LogWarning("Invalid or incomplete order message.");
                    return;
                }

                logger.LogInformation($"Processing order for {order.CustomerName}.");
                // Add your order processing logic here
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process queue message.");
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
