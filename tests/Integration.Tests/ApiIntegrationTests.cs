using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Infrastructure.Persistence;
using Xunit;

namespace TaxiCompare.Integration.Tests;

public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("taxicompare_test")
        .WithUsername("taxi")
        .WithPassword("taxi123")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DB context
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TaxiCompareDbContext>));
                    if (descriptor is not null) services.Remove(descriptor);

                    services.AddDbContext<TaxiCompareDbContext>(opts =>
                        opts.UseNpgsql(_postgres.GetConnectionString()));

                    // Replace Redis
                    services.AddStackExchangeRedisCache(opts =>
                        opts.Configuration = _redis.GetConnectionString());
                });
            });

        _client = _factory.CreateClient();

        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaxiCompareDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    // ── Auth Endpoints ───────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Should_Return_201_With_Token()
    {
        var request = new RegisterRequest("test@taxicompare.dev", "SecurePass1", "Test", "User", null);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be("test@taxicompare.dev");
    }

    [Fact]
    public async Task Register_Then_Login_Should_Succeed()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "SecurePass1", "A", "B", null));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "SecurePass1"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();
        result!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Should_Return_401()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "SecurePass1", "A", "B", null));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "WrongPassword1"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Price Endpoints ──────────────────────────────────────────────────────

    [Fact]
    public async Task Compare_Prices_Should_Return_Results()
    {
        var request = new PriceComparisonRequest(
            "Hauptbahnhof Frankfurt", 50.1071, 8.6640,
            "Frankfurt Airport", 50.0379, 8.5622
        );

        var response = await _client.PostAsJsonAsync("/api/prices/compare", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PriceComparisonResult>();
        result.Should().NotBeNull();
        result!.Prices.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Compare_With_Invalid_Coordinates_Should_Return_400()
    {
        var request = new PriceComparisonRequest("A", 999, 999, "B", 0, 0); // invalid lat/lng
        var response = await _client.PostAsJsonAsync("/api/prices/compare", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_Healthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
