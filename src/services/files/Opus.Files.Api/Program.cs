using Opus.Storage;

var b = WebApplication.CreateBuilder(args);
var cfg = b.Configuration;
var app = b.Build();

var blobFactory = new BlobClientFactory(cfg["BLOB:Endpoint"]!, cfg["BLOB:AccountName"]!, cfg["BLOB:AccountKey"]!);

app.MapPost("/export/parquet/{container}/{name}", async (string container, string name) =>
{
    var data = Enumerable.Range(0, 100).Select(i => new Row(i, DateTimeOffset.UtcNow, Random.Shared.NextDouble()));
    using var ms = new MemoryStream();
    await ParquetWriterUtil.WriteAsync(data, ms);
    ms.Position = 0;
    var c = blobFactory.GetContainer(container);
    await c.CreateIfNotExistsAsync();
    await c.UploadBlobAsync(name, ms);
    return Results.Ok(new { container, name });
});

app.Run();
record Row(int Id, DateTimeOffset Ts, double Value);
