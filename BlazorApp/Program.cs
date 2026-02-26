using BlazorApp.Components;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://otel-collector:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("BlazorGrafanaApp.Blazor"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithLogging(l =>
    {
        l.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://api:8080";
builder.Services.AddHttpClient("Api", client => { client.BaseAddress = new Uri(apiBaseUrl); });
builder.Services.AddScoped<BlazorApp.Services.ApiClient>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("BlazorGrafanaApp.Blazor");
startupLogger.LogInformation("BlazorGrafanaApp.Blazor started");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Only redirect to HTTPS when not in Docker (Docker typically serves HTTP only; redirection breaks asset loading)
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
