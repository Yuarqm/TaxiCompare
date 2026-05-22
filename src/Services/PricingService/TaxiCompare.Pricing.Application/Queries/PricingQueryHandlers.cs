using MediatR;
using TaxiCompare.Pricing.Application.Interfaces;
using TaxiCompare.Pricing.Domain.Interfaces;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Application.Queries;

public class GetPriceHistoryHandler : IRequestHandler<GetPriceHistoryQuery, IReadOnlyList<PriceHistoryPointDto>>
{
    private readonly IPriceSnapshotRepository _snapshotRepo;

    public GetPriceHistoryHandler(IPriceSnapshotRepository snapshotRepo)
        => _snapshotRepo = snapshotRepo;

    public async Task<IReadOnlyList<PriceHistoryPointDto>> Handle(
        GetPriceHistoryQuery query, CancellationToken ct)
    {
        var (from, to) = query.TimeRange switch
        {
            "1h" => (DateTime.UtcNow.AddHours(-1), DateTime.UtcNow),
            "24h" => (DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
            "7d" => (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            _ => (DateTime.UtcNow.AddDays(-1), DateTime.UtcNow)
        };

        var snapshots = await _snapshotRepo.GetHistoryAsync(
            query.Origin, query.Destination, query.ProviderId, from, to, ct);

        return snapshots.Select(s => new PriceHistoryPointDto(
            s.ProviderId.ToString(),
            (s.MinPrice + s.MaxPrice) / 2,
            s.Currency,
            s.SurgeMultiplier,
            s.CapturedAt
        )).ToList();
    }
}

public class GetAiPricePredictionHandler : IRequestHandler<GetAiPricePredictionQuery, PricePredictionDto>
{
    private readonly IAiPricePredictionService _aiService;

    public GetAiPricePredictionHandler(IAiPricePredictionService aiService)
        => _aiService = aiService;

    public Task<PricePredictionDto> Handle(GetAiPricePredictionQuery query, CancellationToken ct)
        => _aiService.PredictOptimalOrderTimeAsync(query.Origin, query.Destination, ct);
}
