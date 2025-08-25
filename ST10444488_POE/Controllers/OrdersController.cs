using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;

namespace ST10444488_POE.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableClient _orderTable;
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;

        public OrdersController()
        {
            string connectionString = "<your_connection_string>";

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
            var customer = _customerTable.GetEntity<Customer>("Customer", order.CustomerId).Value;
            var product = _productTable.GetEntity<Product>("Product", order.ProductId).Value;

            ViewBag.CustomerName = $"{customer.FirstName} {customer.LastName}";
            ViewBag.ProductName = product.Name;

            return View(order);
        }

        public IActionResult Create()
        {
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            order.PartitionKey = "Order";
            order.RowKey = Guid.NewGuid().ToString();
            order.OrderDate = DateTime.Now;

            await _orderTable.AddEntityAsync(order);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string partitionKey, string rowKey)
        {
            var order = _orderTable.GetEntity<Order>(partitionKey, rowKey).Value;
            ViewBag.Customers = _customerTable.Query<Customer>().ToList();
            ViewBag.Products = _productTable.Query<Product>().ToList();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            await _orderTable.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string partitionKey, string rowKey)
        {
            var order = _orderTable.GetEntity<Order>(partitionKey, rowKey).Value;
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _orderTable.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}

