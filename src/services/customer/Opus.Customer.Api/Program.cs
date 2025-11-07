using Raven.Client.Documents;
using Opus.Messaging;

var b = WebApplication.CreateBuilder(args);
var s = b.Services;
s.AddSingleton<IDocumentStore>(_ =>
{
    var store = new DocumentStore
    {
        Urls = new[] { b.Configuration["RAVEN:Urls"]! },
        Database = b.Configuration["RAVEN:Database"]!
    };
    store.Initialize();
    return store;
});
s.AddSingleton<IMessageBus>(_ => new RabbitMqMessageBus(b.Configuration["RABBIT:Uri"]!));
s.AddEndpointsApiExplorer();
s.AddSwaggerGen();
var app = b.Build();
app.MapGet("/customers/{id}", async (string id, IDocumentStore store) =>
{
    using var session = store.OpenAsyncSession();
    var entity = await session.LoadAsync<Customer>(id);
    return entity is null ? Results.NotFound() : Results.Ok(entity);
});
app.MapPost("/customers", async (Customer c, IDocumentStore store, IMessageBus bus) =>
{
    using var session = store.OpenAsyncSession();
    await session.StoreAsync(c);
    await session.SaveChangesAsync();
    await bus.PublishAsync("customer.events", new { type = "created", c.Id });
    return Results.Created($"/customers/{c.Id}", c);
});
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
record Customer(string Id, string Name, string Email);
