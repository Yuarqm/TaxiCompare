# ── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimisation)
COPY TaxiCompare.sln .
COPY src/Domain/TaxiCompare.Domain.csproj               src/Domain/
COPY src/Application/TaxiCompare.Application.csproj     src/Application/
COPY src/Infrastructure/TaxiCompare.Infrastructure.csproj src/Infrastructure/
COPY src/Gateway/TaxiCompare.Gateway.csproj              src/Gateway/
COPY src/Shared/Contracts/TaxiCompare.Contracts.csproj   src/Shared/Contracts/

RUN dotnet restore src/Gateway/TaxiCompare.Gateway.csproj

# Copy remaining source and publish
COPY src/ src/
RUN dotnet publish src/Gateway/TaxiCompare.Gateway.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaxiCompare.Gateway.dll"]
