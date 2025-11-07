using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var b = WebApplication.CreateBuilder(args);

// OpenTelemetry configuration
b.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("opus-workflow"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen();
var app = b.Build();
app.MapPost("/workflow/start/{type}", (string type) => Results.Ok(new { instanceId = Guid.NewGuid(), type }));
app.UseSwagger();
app.UseSwaggerUI();
app.MapPrometheusScrapingEndpoint();
app.Run();
