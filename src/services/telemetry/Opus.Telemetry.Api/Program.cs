using MQTTnet;
using MQTTnet.Client;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var b = WebApplication.CreateBuilder(args);
var cfg = b.Configuration;
var s = b.Services;

// OpenTelemetry configuration
s.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("opus-telemetry"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());
s.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(cfg["PG:ConnectionString"]!));
s.AddEndpointsApiExplorer();
s.AddSwaggerGen();
var app = b.Build();
await using (var ds = app.Services.GetRequiredService<NpgsqlDataSource>())
await using (var c = await ds.OpenConnectionAsync())
await using (var cmd = c.CreateCommand())
{
    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS telemetry(
  ts timestamptz NOT NULL,
  device_id text NOT NULL,
  metric text NOT NULL,
  value double precision NOT NULL
);
SELECT create_hypertable('telemetry', 'ts', if_not_exists => true);
";
    await cmd.ExecuteNonQueryAsync();
}
var mqttFactory = new MqttFactory();
var client = mqttFactory.CreateMqttClient();
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(cfg["MQTT:Broker"], int.Parse(cfg["MQTT:Port"]!))
    .Build();
client.ApplicationMessageReceivedAsync += async e =>
{
    var parts = e.ApplicationMessage.Topic.Split('/');
    if (parts.Length < 3) return;
    var deviceId = parts[1];
    var metric = parts[2];
    if (double.TryParse(e.ApplicationMessage.ConvertPayloadToString(), out var value))
    {
        await using var ds = app.Services.GetRequiredService<NpgsqlDataSource>();
        await using var c = await ds.OpenConnectionAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO telemetry(ts, device_id, metric, value) VALUES(now(), @d, @m, @v)";
        cmd.Parameters.AddWithValue("d", deviceId);
        cmd.Parameters.AddWithValue("m", metric);
        cmd.Parameters.AddWithValue("v", value);
        await cmd.ExecuteNonQueryAsync();
    }
};
await client.ConnectAsync(options);
await client.SubscribeAsync("device/+/+");
app.MapGet("/metrics/latest/{deviceId}", async (string deviceId, NpgsqlDataSource ds) =>
{
    await using var c = await ds.OpenConnectionAsync();
    await using var cmd = c.CreateCommand();
    cmd.CommandText = @"SELECT metric, value, ts FROM telemetry WHERE device_id=@d ORDER BY ts DESC LIMIT 50";
    cmd.Parameters.AddWithValue("d", deviceId);
    await using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync()) list.Add(new { metric = r.GetString(0), value = r.GetDouble(1), ts = r.GetDateTime(2) });
    return list;
});
app.UseSwagger();
app.UseSwaggerUI();
app.MapPrometheusScrapingEndpoint();
app.Run();
