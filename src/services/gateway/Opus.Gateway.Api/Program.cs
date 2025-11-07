var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddCors();

// JWT Authentication with configurable validation
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        var enableValidation = cfg.GetValue<bool>("JWT:EnableValidation");

        if (enableValidation)
        {
            // Production: Full JWT validation
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = cfg["JWT:Issuer"],
                ValidAudience = cfg["JWT:Audience"],
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(cfg["JWT:SigningKey"] ?? ""))
            };
        }
        else
        {
            // Development: No validation (open endpoints)
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
            };
        }
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseCors(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { ok = true }));
app.MapGet("/gateway/ping", () => "pong");
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
