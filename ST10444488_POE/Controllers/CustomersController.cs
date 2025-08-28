using Azure;
using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using System.IO;

namespace ST10444488_POE.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly ShareClient _shareClient;

        public CustomersController(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"];

            _customerTable = new TableClient(connectionString, "CustomerTable");
            _productTable = new TableClient(connectionString, "ProductTable");
            _shareClient = new ShareClient(connectionString, "documentshare");

            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();
            _shareClient.CreateIfNotExists();
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
        public async Task<IActionResult> Create(Customer customer, IFormFile Document)
        {
            customer.RowKey = Guid.NewGuid().ToString();
            customer.PartitionKey = "Customer";

            if (Document != null && Document.Length > 0)
            {
                string fileName = $"{customer.RowKey}_{Path.GetFileName(Document.FileName)}";

                ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
                ShareFileClient fileClient = rootDir.GetFileClient(fileName);

                using var stream = Document.OpenReadStream();
                await fileClient.CreateAsync(stream.Length);

                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length);

                using var uploadStream = new MemoryStream(buffer);
                await fileClient.UploadRangeAsync(new HttpRange(0, buffer.Length), uploadStream);

                customer.Document = fileName;
            }

            await _customerTable.AddEntityAsync(customer);
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

        [HttpPost]
        public async Task<IActionResult> UploadCreditDocument(IFormFile creditDoc, string customerId)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(creditDoc?.FileName ?? "").ToLowerInvariant();

            if (creditDoc == null || creditDoc.Length == 0)
            {
                ViewData["UploadMessage"] = "Please select a file to upload.";
                return View();
            }

            if (!allowedExtensions.Contains(extension))
            {
                ViewData["UploadMessage"] = "Only PDF or Word documents (.pdf, .doc, .docx) are allowed.";
                return View();
            }

            string fileName = $"{customerId}_{Path.GetFileName(creditDoc.FileName)}";

            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
            ShareFileClient fileClient = rootDir.GetFileClient(fileName);

            using var stream = creditDoc.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);

            byte[] buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            using var uploadStream = new MemoryStream(buffer);
            await fileClient.UploadRangeAsync(new HttpRange(0, buffer.Length), uploadStream);

            ViewData["UploadMessage"] = "Document uploaded successfully.";
            return View();
        }
    }
}
