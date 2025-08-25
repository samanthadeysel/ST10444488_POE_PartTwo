using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10444488_POE.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } //order id

        
        public string CustomerId { get; set; }
        public string ProductId { get; set; }

        public int Quantity { get; set; }
        public int TotalCost {get;set;}
        public DateTime OrderDate { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
