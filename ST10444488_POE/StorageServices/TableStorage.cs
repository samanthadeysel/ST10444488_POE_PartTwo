using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ST10444488_POE.Models;

namespace ST10444488_POE.StorageServices
{
    public class TableStorage
    {
        private readonly TableClient _productTable;
        private readonly TableClient _customerTable;

        public TableStorage(string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _productTable = serviceClient.GetTableClient("Products");
            _customerTable = serviceClient.GetTableClient("Customers");

            _productTable.CreateIfNotExists();
            _customerTable.CreateIfNotExists();
        }

        public async Task AddProductAsync(Product product) =>
            await _productTable.AddEntityAsync(product);

        public async Task<List<Product>> GetAllProductsAsync() =>
            _productTable.Query<Product>().ToList();

        public async Task AddCustomerAsync(Customer customer) =>
            await _customerTable.AddEntityAsync(customer);

        public async Task<List<Customer>> GetAllCustomersAsync() =>
            _customerTable.Query<Customer>().ToList();

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey) =>
            await _customerTable.DeleteEntityAsync(partitionKey, rowKey);
    }
}
