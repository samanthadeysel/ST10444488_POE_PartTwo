using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST10444488_POE.Data;
using ST10444488_POE.Models;
using ST10444488_POE.StorageServices;
using System;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMvc().AddSessionStateTempDataProvider();
builder.Services.AddDbContext<ST10444488_POEContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSQL")));

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

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedPhoneNumber = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ST10444488_POEContext>()
.AddDefaultTokenProviders();



var app = builder.Build();

var cultureInfo = new CultureInfo("en-ZA");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
}
else
{
    app.UseExceptionHandler("/Home/Error"); 
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();