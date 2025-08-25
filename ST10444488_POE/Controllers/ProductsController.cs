using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using Azure;
using Azure.Data.Tables;

namespace ST10444488_POE.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableClient _tableClient;

        public ProductsController()
        {
            string connectionString = "<your_connection_string>";
            _tableClient = new TableClient(connectionString, "ProductTable");
            _tableClient.CreateIfNotExists();
        }

        public IActionResult Index()
        {
            var products = _tableClient.Query<Product>().ToList();
            return View(products);
        }

        public IActionResult Details(string partitionKey, string rowKey)
        {
            var product = _tableClient.GetEntity<Product>(partitionKey, rowKey);
            return View(product.Value);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            product.PartitionKey = "Product";
            product.RowKey = Guid.NewGuid().ToString();
            await _tableClient.AddEntityAsync(product);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string partitionKey, string rowKey)
        {
            var product = _tableClient.GetEntity<Product>(partitionKey, rowKey);
            return View(product.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            await _tableClient.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string partitionKey, string rowKey)
        {
            var product = _tableClient.GetEntity<Product>(partitionKey, rowKey);
            return View(product.Value);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }

    }
}
