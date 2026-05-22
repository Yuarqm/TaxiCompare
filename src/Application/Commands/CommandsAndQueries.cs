using MediatR;
using TaxiCompare.Application.DTOs;

namespace TaxiCompare.Application.Commands
{
    public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResult>;
    public record LoginCommand(LoginRequest Request) : IRequest<AuthResult>;
    public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;
    public record CreateRideRequestCommand(Guid UserId, PriceComparisonRequest Request) : IRequest<PriceComparisonResult>;
    public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<bool>;
    public record SetPriceAlertCommand(Guid UserId, Guid RideRequestId, decimal ThresholdPrice) : IRequest<bool>;
    public record OrderRideCommand(Guid UserId, Guid RideRequestId, string ProviderName, string ProviderSlug, string VehicleClass, decimal Price) : IRequest<bool>;
}

namespace TaxiCompare.Application.Queries
{
    using MediatR;
    using TaxiCompare.Application.DTOs;

    public record GetPricesQuery(PriceComparisonRequest Request) : IRequest<PriceComparisonResult>;
    public record GetPriceHistoryQuery(Guid ProviderId, string Period) : IRequest<IEnumerable<PriceHistoryDto>>;
    public record GetPriceHistoryForRouteQuery(double OriginLat, double OriginLng, double DestLat, double DestLng) : IRequest<IEnumerable<PriceHistoryDto>>;
    public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;
    public record GetUserRideHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<IEnumerable<RideRequestSummaryDto>>;
    public record GetAnalyticsSummaryQuery() : IRequest<AnalyticsSummaryDto>;
    public record GetPopularRoutesQuery(int Count = 10) : IRequest<IEnumerable<PopularRouteDto>>;
    public record GetUserNotificationsQuery(Guid UserId) : IRequest<IEnumerable<NotificationDto>>;

    public record RideRequestSummaryDto(
        Guid Id, string OriginAddress, string DestinationAddress,
        DateTime RequestedAt, int PriceSnapshotCount, decimal? LowestPrice,
        string Status,
        string? OrderedProviderName, string? OrderedProviderSlug,
        string? OrderedVehicleClass, decimal? OrderedPrice, DateTime? OrderedAt);
}
