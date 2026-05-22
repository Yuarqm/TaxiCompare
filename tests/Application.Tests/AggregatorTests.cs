using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Infrastructure.Providers;
using Xunit;

namespace TaxiCompare.Application.Tests;

public class PricingAggregatorTests
{
    private static PriceComparisonRequest MakeFrankfurtRequest() => new(
        "Hauptbahnhof Frankfurt", 50.1071, 8.6640,
        "Frankfurt Airport", 50.0379, 8.5622
    );

    [Fact]
    public async Task GetAllPricesAsync_Should_Return_Results_From_All_Available_Providers()
    {
        var mockProvider1 = CreateMockProvider("Uber", "uber", 10.50m);
        var mockProvider2 = CreateMockProvider("Bolt", "bolt", 8.20m);

        var aggregator = new PricingAggregator(
            new[] { mockProvider1.Object, mockProvider2.Object },
            NullLogger<PricingAggregator>.Instance);

        var result = await aggregator.GetAllPricesAsync(MakeFrankfurtRequest());

        result.Should().NotBeNull();
        result.Prices.Should().HaveCount(2);
        result.BestDeal.Should().NotBeNull();
        result.BestDeal!.ProviderSlug.Should().Be("bolt"); // cheaper
    }

    [Fact]
    public async Task GetAllPricesAsync_Should_Mark_Cheapest_As_BestDeal()
    {
        var cheap = CreateMockProvider("Bolt", "bolt", 5.00m);
        var expensive = CreateMockProvider("Uber", "uber", 15.00m);

        var aggregator = new PricingAggregator(
            new[] { cheap.Object, expensive.Object },
            NullLogger<PricingAggregator>.Instance);

        var result = await aggregator.GetAllPricesAsync(MakeFrankfurtRequest());

        var bestDeal = result.Prices.Single(p => p.IsBestDeal);
        bestDeal.ProviderSlug.Should().Be("bolt");
    }

    [Fact]
    public async Task GetAllPricesAsync_Should_Handle_Provider_Failure_Gracefully()
    {
        var working = CreateMockProvider("Bolt", "bolt", 8.00m);
        var failing = new Mock<ITaxiProvider>();
        failing.Setup(p => p.ProviderName).Returns("Failing");
        failing.Setup(p => p.ProviderSlug).Returns("failing");
        failing.Setup(p => p.IsAvailableInRegion(It.IsAny<double>(), It.IsAny<double>())).Returns(true);
        failing.Setup(p => p.GetPriceAsync(It.IsAny<PriceComparisonRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new HttpRequestException("Connection refused"));

        var aggregator = new PricingAggregator(
            new[] { working.Object, failing.Object },
            NullLogger<PricingAggregator>.Instance);

        var act = async () => await aggregator.GetAllPricesAsync(MakeFrankfurtRequest());

        await act.Should().NotThrowAsync();
        var result = await aggregator.GetAllPricesAsync(MakeFrankfurtRequest());
        result.Prices.Should().HaveCount(1); // only the working one
    }

    [Fact]
    public async Task GetAllPricesAsync_Should_Exclude_Unavailable_Providers()
    {
        var available = CreateMockProvider("Bolt", "bolt", 8.00m, isAvailable: true);
        var unavailable = new Mock<ITaxiProvider>();
        unavailable.Setup(p => p.ProviderName).Returns("YandexRU");
        unavailable.Setup(p => p.ProviderSlug).Returns("yandex");
        unavailable.Setup(p => p.IsAvailableInRegion(It.IsAny<double>(), It.IsAny<double>())).Returns(false);

        var aggregator = new PricingAggregator(
            new[] { available.Object, unavailable.Object },
            NullLogger<PricingAggregator>.Instance);

        var result = await aggregator.GetAllPricesAsync(MakeFrankfurtRequest());

        result.Prices.Should().HaveCount(1);
        result.Prices.Single().ProviderSlug.Should().Be("bolt");
    }

    [Fact]
    public async Task GetAllPricesAsync_RetrievedAt_Should_Be_Recent()
    {
        var provider = CreateMockProvider("Uber", "uber", 10.00m);
        var aggregator = new PricingAggregator(new[] { provider.Object }, NullLogger<PricingAggregator>.Instance);

        var before = DateTime.UtcNow;
        var result = await aggregator.GetAllPricesAsync(MakeFrankfurtRequest());

        result.RetrievedAt.Should().BeOnOrAfter(before);
        result.RetrievedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static Mock<ITaxiProvider> CreateMockProvider(string name, string slug, decimal price, bool isAvailable = true)
    {
        var mock = new Mock<ITaxiProvider>();
        mock.Setup(p => p.ProviderName).Returns(name);
        mock.Setup(p => p.ProviderSlug).Returns(slug);
        mock.Setup(p => p.IsAvailableInRegion(It.IsAny<double>(), It.IsAny<double>())).Returns(isAvailable);
        mock.Setup(p => p.GetPriceAsync(It.IsAny<PriceComparisonRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderPriceDto(Guid.NewGuid(), name, slug, $"/logos/{slug}.svg",
                price, "EUR", 5, "Economy", 1.0, 4.5, true, false));
        return mock;
    }
}

public class ValidatorTests
{
    [Theory]
    [InlineData("", "pass1234A", "First", "Last")]
    [InlineData("notanemail", "pass1234A", "First", "Last")]
    [InlineData("a@b.com", "short", "First", "Last")]
    [InlineData("a@b.com", "nouppercase1", "First", "Last")]
    [InlineData("a@b.com", "NoDigits!", "First", "Last")]
    public void RegisterRequest_Should_Fail_Validation_For_Invalid_Inputs(
        string email, string password, string first, string last)
    {
        var validator = new TaxiCompare.Application.Validators.RegisterRequestValidator();
        var request = new RegisterRequest(email, password, first, last, null);
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterRequest_Should_Pass_Validation_For_Valid_Input()
    {
        var validator = new TaxiCompare.Application.Validators.RegisterRequestValidator();
        var request = new RegisterRequest("valid@example.com", "SecurePass1", "John", "Doe", null);
        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
