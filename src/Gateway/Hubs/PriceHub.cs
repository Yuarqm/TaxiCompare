using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;

namespace TaxiCompare.Gateway.Hubs;

/// <summary>SignalR hub for real-time price streaming</summary>
[Authorize]
public class PriceHub : Hub
{
    private readonly IPricingAggregator _aggregator;
    private readonly ILogger<PriceHub> _logger;

    public PriceHub(IPricingAggregator aggregator, ILogger<PriceHub> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to PriceHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from PriceHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Subscribe to live price updates for a route</summary>
    public async Task SubscribeToRoute(PriceComparisonRequest request)
    {
        var groupName = GetGroupName(request);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {Id} subscribed to route {Group}", Context.ConnectionId, groupName);
    }

    public async Task UnsubscribeFromRoute(PriceComparisonRequest request)
    {
        var groupName = GetGroupName(request);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>Immediately fetch prices and send to caller</summary>
    public async Task RequestPrices(PriceComparisonRequest request)
    {
        var result = await _aggregator.GetAllPricesAsync(request);
        await Clients.Caller.SendAsync("PricesUpdated", result);
    }

    private static string GetGroupName(PriceComparisonRequest r) =>
        $"route:{r.OriginLat:F3}:{r.OriginLng:F3}:{r.DestinationLat:F3}:{r.DestinationLng:F3}";
}

/// <summary>Background service that periodically pushes price updates to subscribers</summary>
public class PriceUpdateBackgroundService : BackgroundService
{
    private readonly IHubContext<PriceHub> _hub;
    private readonly IServiceProvider _services;
    private readonly ILogger<PriceUpdateBackgroundService> _logger;

    public PriceUpdateBackgroundService(IHubContext<PriceHub> hub, IServiceProvider services,
        ILogger<PriceUpdateBackgroundService> logger)
    {
        _hub = hub;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            // In production: iterate active subscriptions from Redis and push updates
            _logger.LogDebug("Price update tick");
        }
    }
}
