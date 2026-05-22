using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaxiCompare.Application.Commands;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Application.Queries;

namespace TaxiCompare.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    [ProducesResponseType<AuthResult>(200)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(request), ct);
        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResult>(200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request), ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _mediator.Send(new GetUserByIdQuery(userId), ct);
        return user is not null ? Ok(user) : NotFound();
    }
}

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PricesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Compare prices across all providers (no auth required for quick search)</summary>
    [HttpPost("compare")]
    [ProducesResponseType<PriceComparisonResult>(200)]
    public async Task<IActionResult> Compare([FromBody] PriceComparisonRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPricesQuery(request), ct);
        return Ok(result);
    }

    /// <summary>Compare and save ride request (requires auth)</summary>
    [HttpPost("search")]
    [Authorize]
    [ProducesResponseType<PriceComparisonResult>(200)]
    public async Task<IActionResult> Search([FromBody] PriceComparisonRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new CreateRideRequestCommand(userId, request), ct);
        return Ok(result);
    }

    [HttpGet("history/{providerId}/{period}")]
    [ProducesResponseType<IEnumerable<PriceHistoryDto>>(200)]
    public async Task<IActionResult> History(Guid providerId, string period, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPriceHistoryQuery(providerId, period), ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RidesController : ControllerBase
{
    private readonly IMediator _mediator;
    public RidesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new GetUserRideHistoryQuery(userId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Фиксирует факт заказа такси у провайдера.
    /// Вызывается с фронтенда сразу после перенаправления пользователя на сайт провайдера.
    /// </summary>
    [HttpPost("{rideRequestId}/order")]
    public async Task<IActionResult> PlaceOrder(Guid rideRequestId, [FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var ok = await _mediator.Send(
            new TaxiCompare.Application.Commands.OrderRideCommand(
                userId, rideRequestId,
                request.ProviderName, request.ProviderSlug,
                request.VehicleClass, request.Price), ct);
        return ok ? Ok() : NotFound();
    }
}

public record PlaceOrderRequest(string ProviderName, string ProviderSlug, string VehicleClass, decimal Price);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new GetUserNotificationsQuery(userId), ct);
        return Ok(result);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _mediator.Send(new MarkNotificationReadCommand(id, userId), ct);
        return result ? Ok() : NotFound();
    }
}

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AnalyticsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAnalyticsSummaryQuery(), ct);
        return Ok(result);
    }

    [HttpGet("popular-routes")]
    public async Task<IActionResult> PopularRoutes([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPopularRoutesQuery(count), ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weather;
    public WeatherController(IWeatherService weather) => _weather = weather;

    /// <summary>
    /// Текущая погода и коэффициент наценки для города
    /// GET /api/weather?city=Moscow
    /// </summary>
    [HttpGet]
    [ProducesResponseType<WeatherInfoDto>(200)]
    public async Task<IActionResult> GetWeather([FromQuery] string city, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest("Параметр 'city' обязателен.");

        var condition = await _weather.GetCurrentWeatherAsync(city, ct);
        var dto = new WeatherInfoDto(
            condition.City,
            condition.Type.ToString(),
            condition.GetConditionRu(),
            condition.TemperatureCelsius,
            condition.WindSpeedKmh,
            condition.GetPriceMultiplier()
        );
        return Ok(dto);
    }
}
