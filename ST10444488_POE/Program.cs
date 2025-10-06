using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using ST10444488_POE.StorageServices;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton(sp =>
{
    var conn = Environment.GetEnvironmentVariable("StorageConnection");
    return new BlobStorage(conn, "product-images");
});

builder.Services.AddSingleton(sp =>
{
    var conn = Environment.GetEnvironmentVariable("StorageConnection");
    return new TableStorage(conn);
});

builder.Services.AddSingleton(sp =>
{
    var conn = Environment.GetEnvironmentVariable("StorageConnection");
    return new QueueStorage(conn, "order-queue");
});

builder.Services.AddSingleton(sp =>
{
    var conn = Environment.GetEnvironmentVariable("StorageConnection");
    return new FileStorage(conn, "customer-files", "documents");
});

var app = builder.Build();

// Set culture
var cultureInfo = new CultureInfo("en-ZA");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ✅ Correct method
app.UseRouting();
app.UseAuthorization();

// Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();