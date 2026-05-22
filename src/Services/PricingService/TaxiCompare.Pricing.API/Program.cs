using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TaxiCompare.Pricing.Application.Interfaces;
using TaxiCompare.Pricing.API.Hubs;
using TaxiCompare.Pricing.Domain.Interfaces;
using TaxiCompare.Pricing.Infrastructure.Persistence;
using TaxiCompare.Pricing.Infrastructure.Providers;
using TaxiCompare.Pricing.Infrastructure.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/pricing-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ─── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<PricingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
        o => o.EnableRetryOnFailure(3)));

// ─── Redis ────────────────────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ─── MediatR ──────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<TaxiCompare.Pricing.Application.Commands.GetPriceComparisonCommand>());

// ─── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
builder.Services.AddScoped<IRideRequestRepository, RideRequestRepository>();
builder.Services.AddScoped<IPriceSnapshotRepository, PriceSnapshotRepository>();

// ─── Taxi Providers (with typed HttpClients + Polly) ──────────────────────────
builder.Services.AddHttpClient<UberProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.uber.com/v1.2/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddHttpClient<BoltProvider>(client =>
{
    client.BaseAddress = new Uri("https://node.bolt.eu/booking/taxi/v2/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddHttpClient<YandexProvider>(client =>
{
    client.BaseAddress = new Uri("https://taxi-routeinfo.taxi.yandex.net/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddHttpClient<FreeNowProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.free-now.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddHttpClient<LyftProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.lyft.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddScoped<ITaxiProvider, UberProvider>();
builder.Services.AddScoped<ITaxiProvider, BoltProvider>();
builder.Services.AddScoped<ITaxiProvider, YandexProvider>();
builder.Services.AddScoped<ITaxiProvider, FreeNowProvider>();
builder.Services.AddScoped<ITaxiProvider, LyftProvider>();
builder.Services.AddScoped<IPriceAggregationService, PriceAggregationService>();

// ─── JWT Auth ─────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };
        // Allow JWT in SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

// ─── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// ─── Swagger ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaxiCompare Pricing API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
            Array.Empty<string>()
        }
    });
});

// ─── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ─── Health Checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>())
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PriceHub>("/hubs/prices");
app.MapHealthChecks("/health");

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
