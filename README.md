# 🚕 TaxiCompare

> **Production-ready taxi price comparison platform** built entirely in C# and .NET 8.
> Compare Uber, Bolt, Yandex Taxi, and FREE NOW in real time — find the cheapest ride instantly.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)

---

## ✨ Features

| Feature | Details |
|---|---|
| **Live Price Comparison** | Parallel requests to Uber, Bolt, Yandex, FREE NOW |
| **Best Deal Detection** | Automatically highlights the cheapest available option |
| **Real-time Updates** | SignalR WebSocket hub pushes price changes live |
| **Price History Charts** | Chart.js graphs for 1h / 24h / 7d trends |
| **JWT Authentication** | Register, login, refresh tokens |
| **Price Alerts** | Notify users when prices drop below threshold |
| **Ride History** | Full search history per user |
| **Analytics Dashboard** | Popular routes, average prices, surge heatmap |
| **Dark/Light Theme** | Glassmorphism UI with smooth animations |
| **Kubernetes-ready** | HPA, health checks, rolling deploys |

---

## 🏗️ Architecture

```
TaxiCompare.sln
├── src/
│   ├── Domain/                  # Entities, Value Objects, Domain Events
│   ├── Application/             # CQRS (MediatR), DTOs, Validators, Interfaces
│   ├── Infrastructure/          # EF Core, Redis, Provider Implementations, JWT
│   ├── Gateway/                 # ASP.NET Core 8 API + SignalR Hub
│   ├── Services/
│   │   ├── IdentityService/     # (microservice-ready)
│   │   ├── PricingService/      # (microservice-ready)
│   │   ├── AggregatorService/   # (microservice-ready)
│   │   └── NotificationService/ # (microservice-ready)
│   ├── Frontend/
│   │   └── TaxiCompare.Blazor/  # Blazor WebAssembly SPA
│   └── Shared/
│       └── Contracts/           # Shared event contracts (inter-service)
└── tests/
    ├── Domain.Tests/            # xUnit unit tests (entities, value objects)
    ├── Application.Tests/       # xUnit + Moq (aggregator, validators, handlers)
    └── Integration.Tests/       # WebApplicationFactory + Testcontainers
```

### Design Patterns Used

- **Clean Architecture** — strict layer separation with dependency inversion
- **CQRS + MediatR** — commands and queries fully separated
- **Repository Pattern** — domain stays persistence-ignorant
- **Strategy Pattern** — `ITaxiProvider` abstraction for each taxi service
- **Polly Resilience** — retry, circuit breaker, timeout on all HTTP providers
- **Outbox Pattern** — ready for reliable event publishing (Contracts project)

---

## 🚀 Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) ≥ 24
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local dev)

### Run with Docker Compose

```bash
git clone https://github.com/yourorg/taxicompare.git
cd taxicompare

# Start everything (PostgreSQL + Redis + Gateway + Frontend)
docker compose up -d

# With pgAdmin for DB inspection
docker compose --profile dev up -d
```

| Service    | URL                          |
|------------|------------------------------|
| Frontend   | http://localhost:5000         |
| API        | http://localhost:7000         |
| Swagger UI | http://localhost:7000/swagger |
| pgAdmin    | http://localhost:5050         |
| Health     | http://localhost:7000/health  |

### Local Development

```bash
# 1. Start dependencies only
docker compose up -d postgres redis

# 2. Run the API gateway
cd src/Gateway
dotnet run

# 3. Run the Blazor frontend (separate terminal)
cd src/Frontend/TaxiCompare.Blazor
dotnet run
```

---

## 🗄️ Database Migrations

```bash
# Add new migration
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure \
  --startup-project src/Gateway \
  --output-dir Persistence/Migrations

# Apply migrations
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Gateway

# Apply via Docker
docker compose run --rm gateway dotnet ef database update
```

---

## 🧪 Testing

```bash
# All tests
dotnet test TaxiCompare.sln

# Unit tests only (fast, no Docker needed)
dotnet test tests/Domain.Tests
dotnet test tests/Application.Tests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/Integration.Tests

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
```

---

## 🔌 Adding a New Taxi Provider

1. Implement `ITaxiProvider` in `src/Infrastructure/Providers/`:

```csharp
public class LyftProvider : BaseTaxiProvider
{
    public override string ProviderName => "Lyft";
    public override string ProviderSlug => "lyft";

    public override bool IsAvailableInRegion(double lat, double lng) =>
        lat >= 25 && lat <= 50 && lng >= -125 && lng <= -65; // USA

    public override async Task<ProviderPriceDto?> GetPriceAsync(
        PriceComparisonRequest request, CancellationToken ct = default)
    {
        // Call Lyft Cost Estimate API
        // https://developer.lyft.com/reference/cost-estimates
    }
}
```

2. Register in `Program.cs`:
```csharp
builder.Services.AddHttpClient<LyftProvider>(c => {
    c.BaseAddress = new Uri("https://api.lyft.com");
    c.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<ITaxiProvider, LyftProvider>();
```

3. Seed the `Providers` table in EF migration.

That's it — the aggregator picks it up automatically.

---

## ⚙️ Configuration

Key settings in `appsettings.json` / environment variables:

| Key | Description |
|-----|-------------|
| `ConnectionStrings__Postgres` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__Secret` | ≥32-char secret key (change in prod!) |
| `Jwt__Issuer` | Token issuer |
| `Jwt__Audience` | Token audience |

---

## 🛡️ Security

- **JWT Bearer** tokens with 1h expiry + refresh tokens
- **BCrypt** password hashing (cost factor 12)
- **Rate limiting** per IP (AspNetCoreRateLimit)
- **FluentValidation** on all API inputs
- **HTTPS** enforced in production (via Nginx / ingress)
- **CORS** restricted to known frontend origins
- **EF Core parameterized queries** — SQL injection prevention by default

---

## ☸️ Kubernetes Deployment

```bash
# Apply all manifests
kubectl apply -f docker/kubernetes.yml

# Check status
kubectl get pods -n taxicompare

# Scale gateway
kubectl scale deployment gateway --replicas=5 -n taxicompare
```

The HPA automatically scales the gateway from 2 to 10 replicas based on CPU (70% threshold).

---

## 📊 Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 12 / .NET 8 |
| Backend Framework | ASP.NET Core 8 |
| Frontend | Blazor WebAssembly |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 16 |
| Cache | Redis 7 |
| Real-time | SignalR |
| Messaging (CQRS) | MediatR |
| Validation | FluentValidation |
| Resilience | Polly |
| Auth | JWT Bearer + BCrypt |
| Logging | Serilog |
| Charts | Chart.js |
| Testing | xUnit + FluentAssertions + Moq + Testcontainers |
| CI/CD | GitHub Actions |
| Containers | Docker + Docker Compose + Kubernetes |

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.
