using Microsoft.AspNetCore.Mvc;
using ST10444488_POE.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ST10444488_POE.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public ProductsController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        public async Task<IActionResult> Index()
        {
            var functionUrl = _config["AzureFunctions:GetProductList"];
            var response = await _httpClient.GetAsync(functionUrl);

            if (!response.IsSuccessStatusCode)
            {
                ViewData["Error"] = "Failed to retrieve products.";
                return View(new List<Product>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Product>>(json);
            return View(products);
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var functionUrl = $"{_config["AzureFunctions:GetProductDetails"]}?partitionKey={partitionKey}&rowKey={rowKey}";
            var response = await _httpClient.GetAsync(functionUrl);

            if (!response.IsSuccessStatusCode)
            {
                ViewData["Error"] = "Failed to retrieve product details.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<Product>(json);
            return View(product);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
        {
            product.RowKey = Guid.NewGuid().ToString();
            product.PartitionKey = "Product";

            if (ImageFile != null && ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await ImageFile.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                var uploadRequest = new
                {
                    ProductId = product.RowKey,
                    FileName = ImageFile.FileName,
                    FileData = base64
                };

                var functionUrl = _config["AzureFunctions:UploadProductImage"];
                var json = JsonSerializer.Serialize(uploadRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(functionUrl, content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", $"Image upload failed: {resultJson}");
                    return View(product);
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
                product.ImageUrl = result["imageUrl"];
            }

            ModelState.Clear();
            TryValidateModel(product);

            if (!ModelState.IsValid)
                return View(product);

            var createUrl = _config["AzureFunctions:CreateProduct"];
            var productJson = JsonSerializer.Serialize(product);
            var createContent = new StringContent(productJson, Encoding.UTF8, "application/json");

            var createResponse = await _httpClient.PostAsync(createUrl, createContent);

            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to create product: {error}");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var functionUrl = $"{_config["AzureFunctions:GetProductDetails"]}?partitionKey={partitionKey}&rowKey={rowKey}";
            var response = await _httpClient.GetAsync(functionUrl);

            if (!response.IsSuccessStatusCode)
            {
                ViewData["Error"] = "Failed to retrieve product for editing.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<Product>(json);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            var functionUrl = _config["AzureFunctions:UpdateProduct"];
            var content = new MultipartFormDataContent
            {
                { new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json"), "product" }
            };

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var stream = ImageFile.OpenReadStream();
                content.Add(new StreamContent(stream), "ImageFile", ImageFile.FileName);
            }

            var response = await _httpClient.PutAsync(functionUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update product via Azure Function.");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var functionUrl = $"{_config["AzureFunctions:GetProductDetails"]}?partitionKey={partitionKey}&rowKey={rowKey}";
            var response = await _httpClient.GetAsync(functionUrl);

            if (!response.IsSuccessStatusCode)
            {
                ViewData["Error"] = "Failed to retrieve product for deletion.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<Product>(json);
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var functionUrl = $"{_config["AzureFunctions:DeleteProduct"]}?partitionKey={partitionKey}&rowKey={rowKey}";
            var response = await _httpClient.DeleteAsync(functionUrl);

            if (!response.IsSuccessStatusCode)
            {
                ViewData["Error"] = "Failed to delete product via Azure Function.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}