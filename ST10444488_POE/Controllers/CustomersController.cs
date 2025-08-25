using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using Azure.Data.Tables;

namespace ST10444488_POE.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableClient _tableClient;

        public CustomersController()
        {
            _tableClient = new TableClient("<connection_string>", "CustomerTable");
            _tableClient.CreateIfNotExists();
        }

        public IActionResult Index()
        {
            var customers = _tableClient.Query<Customer>().ToList();
            return View(customers);
        }

        // GET: Customers/Details/5
        public IActionResult Details(string partitionKey, string rowKey)
        {
            var customer = _tableClient.GetEntity<Customer>(partitionKey, rowKey);
            return View(customer.Value);
        }


        // GET: Customers/Create
        public IActionResult Create() => View();

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString();
            await _tableClient.AddEntityAsync(customer);
            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Edit/5
        public IActionResult Edit(string partitionKey, string rowKey)
        {
            var customer = _tableClient.GetEntity<Customer>(partitionKey, rowKey);
            return View(customer.Value);
        }

        // POST: Customers/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            await _tableClient.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);
            return RedirectToAction(nameof(Index));
        }


        // GET: Customers/Delete
        public IActionResult Delete(string partitionKey, string rowKey)
        {
            var customer = _tableClient.GetEntity<Customer>(partitionKey, rowKey);
            return View(customer.Value);
        }

        // POST: Customers/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }

    }
}
