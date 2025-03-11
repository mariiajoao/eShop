using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
// using OpenTelemetry.Exporter.Prometheus;
// using OpenTelemetry.Exporter.Prometheus.AspNetCore;
using OpenTelemetry.Instrumentation.SqlClient;
using eShop.Ordering.API.Application.Telemetry;
using eShop.ServiceDefaults.Telemetry;

var builder = WebApplication.CreateBuilder(args);

var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
var otel = builder.Services.AddOpenTelemetry();

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

otel.ConfigureResource(resource => resource
      .AddService("ordering-api"));

otel.WithMetrics(metrics => metrics
      .AddAspNetCoreInstrumentation()
      .AddRuntimeInstrumentation()
      .AddPrometheusExporter()
      .AddMeter("Microsoft.AspNetCore.Hosting")
      .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
      .AddMeter("System.Net.Http")
      .AddMeter("System.Net.NameResolution"));

otel.WithTracing(tracing =>
{
      tracing.AddProcessor(new PiiScrubberProcessor());
      tracing.AddAspNetCoreInstrumentation();
      tracing.AddHttpClientInstrumentation();
      tracing.AddSource("eShop.WebApp");
      if (tracingOtlpEndpoint != null)
      {
            tracing.AddOtlpExporter(otlpOptions =>
                  {
                        otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
                  });
      }
      else
      {
            tracing.AddConsoleExporter();
      }
});


builder.Services.AddSingleton<OrderingTelemetry>();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();
// app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");

orders.MapOrdersApiV1()
      .RequireAuthorization();


if (!app.Environment.IsDevelopment())
{
      app.UseExceptionHandler("/Error");
      app.UseHsts();
}

app.UseDefaultOpenApi();

app.UseAntiforgery();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.Run();
