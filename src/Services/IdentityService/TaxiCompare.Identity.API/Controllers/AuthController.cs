using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaxiCompare.Identity.Application.Commands;

namespace TaxiCompare.Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthController(ISender mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Email, request.Name, request.Password), ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        if (!result.Success) return Unauthorized(new { error = result.Error });
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request.Token), ct);
        if (!result.Success) return Unauthorized(new { error = result.Error });
        return Ok(result);
    }
}

public record RegisterRequest(string Email, string Name, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string Token);
