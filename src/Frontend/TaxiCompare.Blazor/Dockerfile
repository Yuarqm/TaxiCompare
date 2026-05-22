# ── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TaxiCompare.sln .
COPY src/Frontend/TaxiCompare.Blazor/TaxiCompare.Blazor.csproj src/Frontend/TaxiCompare.Blazor/

RUN dotnet restore src/Frontend/TaxiCompare.Blazor/TaxiCompare.Blazor.csproj

COPY src/Frontend/TaxiCompare.Blazor/ src/Frontend/TaxiCompare.Blazor/

RUN dotnet publish src/Frontend/TaxiCompare.Blazor/TaxiCompare.Blazor.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime Stage (Nginx) ──────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/Frontend/TaxiCompare.Blazor/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
