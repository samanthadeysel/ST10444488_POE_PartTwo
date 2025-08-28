using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10444488_POE.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } //order id

        public string CustomerRowKey { get; set; }
        public string FirstName {  get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }

        public string ProductRowKeys { get; set; }
        public string ProductNames { get; set; }

        public int Quantity { get; set; }
        public decimal TotalCost {get;set;}
        public DateTime OrderDate { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
