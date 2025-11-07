var b = WebApplication.CreateBuilder(args);
b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen();
var app = b.Build();
app.MapPost("/workflow/start/{type}", (string type) => Results.Ok(new { instanceId = Guid.NewGuid(), type }));
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
