using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;

namespace ST10444488_POE.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableClient _productTable;
        private readonly BlobContainerClient _blobContainer;
        private readonly IConfiguration _config;

        public ProductsController(IConfiguration config)
        {
            _config = config;

            string connectionString = _config["AzureStorage:ConnectionString"];
            _productTable = new TableClient(connectionString, "ProductTable");
            _blobContainer = new BlobContainerClient(connectionString, "productimages");

            _productTable.CreateIfNotExists();
            _blobContainer.CreateIfNotExists();
        }

        public IActionResult Index()
        {
            var products = _productTable.Query<Product>().ToList();
            return View(products);
        }

        public IActionResult Details(string partitionKey, string rowKey) =>
            View(_productTable.GetEntity<Product>(partitionKey, rowKey).Value);

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.PartitionKey = "Product";
            product.RowKey = Guid.NewGuid().ToString();

            if (ImageFile == null || ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Please upload a valid image.");
                return View(product);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ImageFile", "Only JPG and PNG files are allowed.");
                return View(product);
            }

            try
            {
                var blobName = $"{product.RowKey}{extension}";
                var blobClient = _blobContainer.GetBlobClient(blobName);

                using var stream = ImageFile.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                product.ImageUrl = blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Image upload failed: {ex.Message}");
                return View(product);
            }

            try
            {
                await _productTable.AddEntityAsync(product);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Product save failed: {ex.Message}");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string partitionKey, string rowKey) =>
            View(_productTable.GetEntity<Product>(partitionKey, rowKey).Value);

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Only JPG and PNG files are allowed.");
                    return View(product);
                }

                try
                {
                    var blobName = $"{product.RowKey}{extension}";
                    var blobClient = _blobContainer.GetBlobClient(blobName);

                    using var stream = ImageFile.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);

                    product.ImageUrl = blobClient.Uri.ToString();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Image upload failed: {ex.Message}");
                    return View(product);
                }
            }

            await _productTable.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string partitionKey, string rowKey) =>
            View(_productTable.GetEntity<Product>(partitionKey, rowKey).Value);

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _productTable.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }

}
