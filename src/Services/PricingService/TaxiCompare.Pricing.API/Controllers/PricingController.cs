using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiCompare.Pricing.Application.Commands;
using TaxiCompare.Pricing.Application.Queries;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly ISender _mediator;

    public PricingController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Compare prices across all providers for a given route.
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(PriceComparisonResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare(
        [FromBody] CompareRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value ?? "anonymous";
        var result = await _mediator.Send(new GetPriceComparisonCommand(
            userId,
            request.Origin, request.OriginLat, request.OriginLng,
            request.Destination, request.DestinationLat, request.DestinationLng,
            request.PreferredClass), ct);

        return Ok(result);
    }

    /// <summary>
    /// Get price history for a route over time.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<PriceHistoryPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] string? providerId,
        [FromQuery] string timeRange = "24h",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetPriceHistoryQuery(origin, destination, providerId, timeRange), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get AI-powered price prediction and optimal booking time.
    /// </summary>
    [HttpGet("predict")]
    [ProducesResponseType(typeof(PricePredictionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrediction(
        [FromQuery] string origin,
        [FromQuery] string destination,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAiPricePredictionQuery(origin, destination), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get popular routes analytics.
    /// </summary>
    [HttpGet("popular-routes")]
    [ProducesResponseType(typeof(IReadOnlyList<PopularRouteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularRoutes(
        [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPopularRoutesQuery(limit), ct);
        return Ok(result);
    }
}

public record CompareRequest(
    string Origin,
    double OriginLat,
    double OriginLng,
    string Destination,
    double DestinationLat,
    double DestinationLng,
    string? PreferredClass = null
);
