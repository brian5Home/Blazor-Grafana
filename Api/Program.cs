using BlazorGrafanaApp.Core.Data;
using BlazorGrafanaApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://otel-collector:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("BlazorGrafanaApp.Api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true);
        t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithLogging(l =>
    {
        l.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        l.AddConsoleExporter(); // so "docker compose logs api" shows logs; confirms provider is active
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Name = "Widget A", Description = "Standard widget", Category = "Widgets", UnitPrice = 9.99m, StockQuantity = 100 },
            new Product { Name = "Gadget B", Description = "Premium gadget", Category = "Gadgets", UnitPrice = 24.99m, StockQuantity = 50 },
            new Product { Name = "Tool C", Description = "Basic tool", Category = "Tools", UnitPrice = 14.99m, StockQuantity = 5 }
        );
        await db.SaveChangesAsync();
    }
}

app.MapGet("/api/products", async (AppDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    logger.LogInformation("Listing all products");
    return await db.Products.OrderBy(p => p.Name).ToListAsync(ct);
});

app.MapGet("/api/products/{id:int}", async (int id, AppDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    var product = await db.Products.FindAsync([id], ct);
    if (product is null) { logger.LogWarning("Product {ProductId} not found", id); return Results.NotFound(); }
    logger.LogInformation("Returning product {ProductId} {Name}", product.Id, product.Name);
    return Results.Ok(product);
});

app.MapPost("/api/products", async (Product product, AppDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    product.CreatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync(ct);
    logger.LogInformation("Created product {ProductId} {Name}", product.Id, product.Name);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id:int}", async (int id, Product input, AppDbContext db, CancellationToken ct) =>
{
    var product = await db.Products.FindAsync([id], ct);
    if (product is null) return Results.NotFound();
    product.Name = input.Name;
    product.Description = input.Description;
    product.Category = input.Category;
    product.UnitPrice = input.UnitPrice;
    product.StockQuantity = input.StockQuantity;
    product.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(product);
});

app.MapDelete("/api/products/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var product = await db.Products.FindAsync([id], ct);
    if (product is null) return Results.NotFound();
    db.Products.Remove(product);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.MapGet("/api/reports/summary", async (AppDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    logger.LogInformation("Generating report summary");
    var totalProducts = await db.Products.CountAsync(ct);
    var byCategory = await db.Products
        .GroupBy(p => p.Category)
        .Select(g => new { Category = g.Key, Count = g.Count(), TotalValue = g.Sum(p => p.UnitPrice * p.StockQuantity) })
        .ToListAsync(ct);
    var lowStock = await db.Products.Where(p => p.StockQuantity < 10).CountAsync(ct);
    return Results.Ok(new
    {
        TotalProducts = totalProducts,
        ByCategory = byCategory,
        LowStockCount = lowStock
    });
});

app.Run();
