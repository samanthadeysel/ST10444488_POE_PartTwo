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
    public class OrdersController : Controller
    {
        private readonly TableClient _orderTable;
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly IConfiguration _config;

        public OrdersController(IConfiguration config)
        {
            _config = config;
            string connectionString = config["AzureStorage:ConnectionString"];
            _orderTable = new TableClient(connectionString, "OrderTable");
            _customerTable = new TableClient(connectionString, "CustomerTable");
            _productTable = new TableClient(connectionString, "ProductTable");

            _orderTable.CreateIfNotExists();
            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();
        }

        public IActionResult Index()
        {
            var orders = _orderTable.Query<Order>().ToList();
            return View(orders);
        }

        public IActionResult Details(string partitionKey, string rowKey)
        {
            var order = _orderTable.GetEntity<Order>(partitionKey, rowKey).Value;
            return View(order);
        }

        public IActionResult Create()
        {
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();
            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();

            var selectedKeys = order.ProductRowKeys?.Split(',') ?? Array.Empty<string>();
            var selectedProducts = _productTable.Query<Product>()
                .Where(p => selectedKeys.Contains(p.RowKey))
                .ToList();

            order.Quantity = selectedProducts.Count;
            order.TotalCost = selectedProducts.Sum(p => (decimal)p.Price);
            order.ProductNames = string.Join(", ", selectedProducts.Select(p => p.Name));

            if (string.IsNullOrEmpty(order.CustomerRowKey) || selectedProducts.Count == 0)
            {
                ModelState.AddModelError("", "Please select a customer and at least one product.");
                return View(order);
            }

            var customer = _customerTable.GetEntity<Customer>("Customer", order.CustomerRowKey).Value;
            order.FirstName = customer.FirstName;
            order.LastName = customer.LastName;
            order.Address = customer.Address;

            order.RowKey = Guid.NewGuid().ToString();
            order.PartitionKey = "Order";
            order.OrderDate = DateTime.Now;

            await _orderTable.AddEntityAsync(order);

            var queueClient = new QueueClient(_config["AzureStorage:ConnectionString"], "order-queue");
            await queueClient.CreateIfNotExistsAsync();
            var queueMessage = JsonSerializer.Serialize(order);
            await queueClient.SendMessageAsync(queueMessage);

            return RedirectToAction("Details", new { partitionKey = order.PartitionKey, rowKey = order.RowKey });
        }

        public IActionResult Edit(string partitionKey, string rowKey)
        {
            var order = _orderTable.GetEntity<Order>(partitionKey, rowKey).Value;
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Order updated)
        {
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();

            var existing = await _orderTable.GetEntityAsync<Order>(updated.PartitionKey, updated.RowKey);
            var order = existing.Value;

            var selectedKeys = updated.ProductRowKeys?.Split(',') ?? Array.Empty<string>();
            var selectedProducts = _productTable.Query<Product>()
                .Where(p => selectedKeys.Contains(p.RowKey))
                .ToList();

            order.CustomerRowKey = updated.CustomerRowKey;
            order.ProductRowKeys = updated.ProductRowKeys;
            order.ProductNames = string.Join(", ", selectedProducts.Select(p => p.Name));
            order.Quantity = selectedProducts.Count;
            order.TotalCost = selectedProducts.Sum(p => (decimal)p.Price);
            order.OrderDate = updated.OrderDate;

            var customer = _customerTable.GetEntity<Customer>("Customer", updated.CustomerRowKey).Value;
            order.FirstName = customer.FirstName;
            order.LastName = customer.LastName;
            order.Address = customer.Address;

            await _orderTable.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string partitionKey, string rowKey)
        {
            var order = _orderTable.GetEntity<Order>(partitionKey, rowKey).Value;
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _orderTable.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}