using BlazorApp.Models;

namespace BlazorApp.Services;

public class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ApiClient> _logger;
    private readonly ActivityTagService _activityTagService;

    public ApiClient(IHttpClientFactory factory, ILogger<ApiClient> logger, ActivityTagService activityTagService)
    {
        _factory = factory;
        _logger = logger;
        _activityTagService = activityTagService;
    }

    private HttpClient Client => _factory.CreateClient("Api");

    public async Task<List<Product>> GetProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Calling API {Route} with activity tag {ActivityTag}", "/api/products", _activityTagService.GetOrCreate());
        return await Client.GetFromJsonAsync<List<Product>>("/api/products", ct) ?? new List<Product>();
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Calling API {Route} for product {ProductId} with activity tag {ActivityTag}", "/api/products/{id}", id, _activityTagService.GetOrCreate());
        return await Client.GetFromJsonAsync<Product>($"/api/products/{id}", ct);
    }

    public async Task<Product?> CreateProductAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("Calling API {Route} to create {Name} with activity tag {ActivityTag}", "/api/products", product.Name, _activityTagService.GetOrCreate());
        var response = await Client.PostAsJsonAsync("/api/products", product, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product>(ct);
    }

    public async Task<Product?> UpdateProductAsync(int id, Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("Calling API {Route} to update {ProductId} with activity tag {ActivityTag}", "/api/products/{id}", id, _activityTagService.GetOrCreate());
        var response = await Client.PutAsJsonAsync($"/api/products/{id}", product, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product>(ct);
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Calling API {Route} to delete {ProductId} with activity tag {ActivityTag}", "/api/products/{id}", id, _activityTagService.GetOrCreate());
        var response = await Client.DeleteAsync($"/api/products/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ReportSummary?> GetReportSummaryAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Calling API {Route} with activity tag {ActivityTag}", "/api/reports/summary", _activityTagService.GetOrCreate());
        return await Client.GetFromJsonAsync<ReportSummary>("/api/reports/summary", ct);
    }
}

public class ReportSummary
{
    public int TotalProducts { get; set; }
    public List<CategorySummary> ByCategory { get; set; } = new();
    public int LowStockCount { get; set; }
}

public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}
