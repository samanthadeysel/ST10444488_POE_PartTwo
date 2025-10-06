using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using System.Text.Json;
using System.Text;
using System.Net.Http;

namespace ST10444488_POE.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly QueueClient _queueClient;
        private readonly IConfiguration _config;

        public CustomersController(IConfiguration configuration)
        {
            _config = configuration;
            string connectionString = configuration["AzureStorage:ConnectionString"];

            _customerTable = new TableClient(connectionString, "CustomerTable");
            _productTable = new TableClient(connectionString, "ProductTable");
            _queueClient = new QueueClient(connectionString, "customer-queue");

            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();
            _queueClient.CreateIfNotExists();
        }

        public IActionResult Index() =>
            View(_customerTable.Query<Customer>().ToList());

        public IActionResult Details(string partitionKey, string rowKey)
        {
            var customer = _customerTable.GetEntity<Customer>(partitionKey, rowKey).Value;
            return View(customer);
        }

        public IActionResult Create()
        {
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            customer.RowKey = Guid.NewGuid().ToString();
            customer.PartitionKey = "Customer";

            await _customerTable.AddEntityAsync(customer);

            // 🔗 Enqueue customer for Azure Function to process
            var message = JsonSerializer.Serialize(customer);
            await _queueClient.SendMessageAsync(message);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string partitionKey, string rowKey)
        {
            var customer = _customerTable.GetEntity<Customer>(partitionKey, rowKey).Value;
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer updated)
        {
            try
            {
                var response = await _customerTable.GetEntityAsync<Customer>(updated.PartitionKey, updated.RowKey);
                var customer = response.Value;

                customer.FirstName = updated.FirstName;
                customer.LastName = updated.LastName;
                customer.Email = updated.Email;
                customer.Cellnumber = updated.Cellnumber;
                customer.Address = updated.Address;
                customer.Document = updated.Document;

                await _customerTable.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(updated);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string partitionKey, string rowKey)
        {
            var customer = _customerTable.GetEntity<Customer>(partitionKey, rowKey).Value;
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _customerTable.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}