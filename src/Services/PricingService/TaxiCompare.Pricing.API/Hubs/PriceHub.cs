using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaxiCompare.Pricing.Application.Commands;

namespace TaxiCompare.Pricing.API.Hubs;

/// <summary>
/// SignalR hub for real-time price updates.
/// Clients subscribe to price streams and receive live updates.
/// </summary>
[Authorize]
public class PriceHub : Hub
{
    private readonly ISender _mediator;
    private readonly ILogger<PriceHub> _logger;

    public PriceHub(ISender mediator, ILogger<PriceHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Start watching a route for live price updates every 30 seconds.
    /// </summary>
    public async Task WatchPrices(WatchPricesRequest request)
    {
        var groupName = GetGroupName(request.OriginLat, request.OriginLng,
            request.DestLat, request.DestLng);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("Client {ConnectionId} watching prices for group {Group}",
            Context.ConnectionId, groupName);

        // Immediately send current prices
        await FetchAndSendPrices(request, groupName);
    }

    public async Task StopWatching(WatchPricesRequest request)
    {
        var groupName = GetGroupName(request.OriginLat, request.OriginLng,
            request.DestLat, request.DestLng);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    private async Task FetchAndSendPrices(WatchPricesRequest request, string groupName)
    {
        try
        {
            var userId = Context.UserIdentifier ?? "anonymous";
            var result = await _mediator.Send(new GetPriceComparisonCommand(
                userId,
                request.Origin, request.OriginLat, request.OriginLng,
                request.Destination, request.DestLat, request.DestLng));

            await Clients.Group(groupName).SendAsync("PricesUpdated", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices for SignalR group {Group}", groupName);
            await Clients.Caller.SendAsync("PriceError", "Failed to fetch prices. Retrying...");
        }
    }

    private static string GetGroupName(double oLat, double oLng, double dLat, double dLng)
        => $"prices:{oLat:F3}:{oLng:F3}:{dLat:F3}:{dLng:F3}";
}

public record WatchPricesRequest(
    string Origin,
    double OriginLat,
    double OriginLng,
    string Destination,
    double DestLat,
    double DestLng
);
