using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Application.Validators;
using TaxiCompare.Infrastructure.Caching;
using TaxiCompare.Infrastructure.Persistence;
using TaxiCompare.Infrastructure.Providers;
using TaxiCompare.Infrastructure.Repositories;
using TaxiCompare.Infrastructure.Security;
using TaxiCompare.Domain.Interfaces;
using FluentValidation;
using MediatR;
using TaxiCompare.Gateway.Hubs;
using TaxiCompare.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<TaxiCompareDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// ── Redis ─────────────────────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRideRequestRepository, RideRequestRepository>();
builder.Services.AddScoped<IPriceSnapshotRepository, PriceSnapshotRepository>();
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IPricingAggregator, PricingAggregator>();

// Заглушка для IPriceAlertService (реальные уведомления не реализованы)
builder.Services.AddScoped<TaxiCompare.Application.Interfaces.IPriceAlertService, TaxiCompare.Infrastructure.Services.NoOpPriceAlertService>();

// ── Taxi Providers ────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<UberProvider>(c => { c.BaseAddress = new Uri("https://api.uber.com"); c.Timeout = TimeSpan.FromSeconds(5); });
builder.Services.AddHttpClient<YandexProvider>(c => { c.BaseAddress = new Uri("https://fleet-api.taxi.yandex.net"); c.Timeout = TimeSpan.FromSeconds(5); });
builder.Services.AddHttpClient<BoltProvider>(c => { c.BaseAddress = new Uri("https://node.bolt.eu"); c.Timeout = TimeSpan.FromSeconds(5); });
builder.Services.AddHttpClient<FreeNowProvider>(c => { c.BaseAddress = new Uri("https://api.free-now.com"); c.Timeout = TimeSpan.FromSeconds(5); });

builder.Services.AddScoped<ITaxiProvider, UberProvider>();
builder.Services.AddScoped<ITaxiProvider, YandexProvider>();
builder.Services.AddScoped<ITaxiProvider, BoltProvider>();
builder.Services.AddScoped<ITaxiProvider, FreeNowProvider>();

// ── MediatR ───────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(TaxiCompare.Application.Commands.RegisterCommand).Assembly));

// ── Validation ────────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ── JWT Auth ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] Auth failed: {ctx.Exception.GetType().Name}: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine($"[JWT] Challenge — path: {ctx.Request.Path}, header: {ctx.Request.Headers["Authorization"]}");
                return Task.CompletedTask;
            }
        };
    });

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Weather Service ────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IWeatherService, TaxiCompare.Infrastructure.Services.WeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "TaxiCompare/1.0");
});
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("BlazorPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://taxicompare-2iv0.onrender.com",
                "https://taxicompare-gateway.onrender.com",
                "https://localhost:7001",
                "http://localhost:5001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaxiCompare API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header, Description = "JWT Bearer token",
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseIpRateLimiting();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check — используется фронтендом для ожидания пробуждения сервиса на Render
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));
app.MapHub<PriceHub>("/hubs/prices");
app.MapHealthChecks("/health");

// Auto-create database schema on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TaxiCompareDbContext>();
    // Создаём таблицы если их нет
    await db.Database.EnsureCreatedAsync();

    // Добавляем новые колонки напрямую через Npgsql — IF NOT EXISTS гарантирует идемпотентность
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        ALTER TABLE ""RideRequests"" ADD COLUMN IF NOT EXISTS ""OrderedProviderName"" text NULL;
        ALTER TABLE ""RideRequests"" ADD COLUMN IF NOT EXISTS ""OrderedProviderSlug"" text NULL;
        ALTER TABLE ""RideRequests"" ADD COLUMN IF NOT EXISTS ""OrderedVehicleClass"" text NULL;
        ALTER TABLE ""RideRequests"" ADD COLUMN IF NOT EXISTS ""OrderedPrice"" numeric NULL;
        ALTER TABLE ""RideRequests"" ADD COLUMN IF NOT EXISTS ""OrderedAt"" timestamp with time zone NULL;
    ";
    await cmd.ExecuteNonQueryAsync();
    await conn.CloseAsync();
    Console.WriteLine("[STARTUP] DB columns ensured.");

    if (!db.Providers.Any())
    {
        db.Providers.AddRange(
            TaxiCompare.Domain.Entities.Provider.Create("Uber",         "uber",    "/logos/uber.svg",    4.7),
            TaxiCompare.Domain.Entities.Provider.Create("Яндекс Такси", "yandex",  "/logos/yandex.svg",  4.6),
            TaxiCompare.Domain.Entities.Provider.Create("Омега",         "omega",   "/logos/omega.svg",   4.4),
            TaxiCompare.Domain.Entities.Provider.Create("Такси Максим",  "maksim",  "/logos/maksim.svg",  4.3)
        );
        await db.SaveChangesAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] DB init error: {ex.Message}");
}

app.Run();
