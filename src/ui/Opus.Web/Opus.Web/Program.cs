using Opus.Web.Client.Pages;
using Opus.Web.Components;
using Opus.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure HttpClient for API calls
var gatewayUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "http://localhost:8088";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(gatewayUrl) });

// Register API service
builder.Services.AddScoped<ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Opus.Web.Client._Imports).Assembly);

app.Run();
