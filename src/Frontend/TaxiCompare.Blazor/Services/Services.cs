using System.Net.Http.Json;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using System.Security.Claims;
using System.Text.Json;


namespace TaxiCompare.Blazor.Services;

// ─── Models ──────────────────────────────────────────────────────────────────
public record PriceComparisonRequest(string OriginAddress, double OriginLat, double OriginLng,
    string DestinationAddress, double DestinationLat, double DestinationLng,
    string? PreferredClass = null, double? DistanceKm = null, string? OriginCity = null);
public record PriceComparisonResult(Guid RideRequestId, List<ProviderPriceDto> Prices,
    ProviderPriceDto? BestDeal, DateTime RetrievedAt);
public record ProviderPriceDto(Guid ProviderId, string ProviderName, string ProviderSlug,
    string LogoUrl, decimal Price, string Currency, int EtaMinutes, string VehicleClass,
    double SurgeMultiplier, double ProviderRating, bool IsAvailable, bool IsBestDeal);
public record WeatherInfoDto(
    string City, string Condition, string ConditionRu,
    double TemperatureCelsius, double WindSpeedKmh, decimal Multiplier)
{
    public bool HasSurcharge => Multiplier > 1.0m;
    public int SurchargePercent => HasSurcharge ? (int)Math.Round((Multiplier - 1.0m) * 100) : 0;
}
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);
public record AuthResult(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
public record UserDto(Guid Id, string Email, string FirstName, string LastName, string? PhoneNumber, DateTime CreatedAt);
public record NotificationDto(Guid Id, string Title, string Message, string Type, bool IsRead, DateTime CreatedAt);

// ─── Auth State Provider ─────────────────────────────────────────────────────
// ─── Auth Token Handler ───────────────────────────────────────────────────────
// Перехватывает каждый HTTP-запрос и подставляет Bearer токен из localStorage.
// Это надёжнее чем DefaultRequestHeaders — работает даже при холодном старте.
public class AuthTokenHandler : DelegatingHandler
{
    private readonly ILocalStorageService _storage;
    public AuthTokenHandler(ILocalStorageService storage) => _storage = storage;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Не перезаписываем если заголовок уже установлен явно
        if (request.Headers.Authorization == null)
        {
            try
            {
                var token = await _storage.GetItemAsync<string>("auth_token");
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            catch { /* localStorage недоступен — продолжаем без токена */ }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

// ─── JWT Auth State Provider ──────────────────────────────────────────────────
public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;
    private readonly HttpClient _http;

    public JwtAuthStateProvider(ILocalStorageService storage, HttpClient http)
    {
        _storage = storage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsync<string>("auth_token");
        if (string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Всегда обновляем заголовок — это защищает от race condition
        // когда страница рендерится до завершения GetAuthenticationStateAsync
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(payload)));
        var kvp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return kvp.Select(kv => new Claim(kv.Key, kv.Value.ToString()!));
    }

    private static string PadBase64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return (s.Length % 4) switch { 2 => s + "==", 3 => s + "=", _ => s };
    }
}

// ─── Auth Service ─────────────────────────────────────────────────────────────
public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password);
    Task<AuthResult?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly JwtAuthStateProvider _authProvider;

    public AuthService(HttpClient http, ILocalStorageService storage, AuthenticationStateProvider authProvider)
    {
        _http = http;
        _storage = storage;
        _authProvider = (JwtAuthStateProvider)authProvider;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        if (result is null) return null;
        await _storage.SetItemAsync("auth_token", result.AccessToken);
        await _storage.SetItemAsync("refresh_token", result.RefreshToken);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        _authProvider.NotifyAuthStateChanged();
        return result;
    }

    public async Task<AuthResult?> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
        }
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        if (result is null) return null;
        await _storage.SetItemAsync("auth_token", result.AccessToken);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        _authProvider.NotifyAuthStateChanged();
        return result;
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync("auth_token");
        _http.DefaultRequestHeaders.Authorization = null;
        _authProvider.NotifyAuthStateChanged();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _storage.GetItemAsync<string>("auth_token");
        return !string.IsNullOrEmpty(token);
    }
}

// ─── Price Service ────────────────────────────────────────────────────────────
public interface IPriceService
{
    Task<PriceComparisonResult?> ComparePricesAsync(PriceComparisonRequest request);
    Task<PriceComparisonResult?> SearchPricesAsync(PriceComparisonRequest request);
}

public class PriceService : IPriceService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;

    public PriceService(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    public async Task<PriceComparisonResult?> ComparePricesAsync(PriceComparisonRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/prices/compare", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PriceComparisonResult>()
            : null;
    }

    public async Task<PriceComparisonResult?> SearchPricesAsync(PriceComparisonRequest request)
    {
        // Токен подставляется автоматически через AuthTokenHandler
        var response = await _http.PostAsJsonAsync("/api/prices/search", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PriceComparisonResult>()
            : null;
    }
}

// ─── SignalR Service ──────────────────────────────────────────────────────────
public interface ISignalRService
{
    event Action<PriceComparisonResult>? OnPricesUpdated;
    Task ConnectAsync(string token);
    Task SubscribeToRouteAsync(PriceComparisonRequest request);
    Task DisconnectAsync();
}

public class SignalRService : ISignalRService, IAsyncDisposable
{
    private HubConnection? _hub;
    public event Action<PriceComparisonResult>? OnPricesUpdated;

    public async Task ConnectAsync(string token)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl($"https://localhost:7000/hubs/prices?access_token={token}")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<PriceComparisonResult>("PricesUpdated", result => OnPricesUpdated?.Invoke(result));
        await _hub.StartAsync();
    }

    public async Task SubscribeToRouteAsync(PriceComparisonRequest request)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.SendAsync("SubscribeToRoute", request);
    }

    public async Task DisconnectAsync()
    {
        if (_hub is not null) await _hub.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }
}

// ─── Notification Service ─────────────────────────────────────────────────────
public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync();
    Task MarkReadAsync(Guid id);
}

public class NotificationClientService : INotificationService
{
    private readonly HttpClient _http;
    public NotificationClientService(HttpClient http) => _http = http;

    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        return result ?? new();
    }

    public async Task MarkReadAsync(Guid id) =>
        await _http.PatchAsync($"/api/notifications/{id}/read", null);
}

// ─── Weather Service (Client) ─────────────────────────────────────────────────
public interface IWeatherClientService
{
    Task<WeatherInfoDto?> GetWeatherAsync(string city);
}

public class WeatherClientService : IWeatherClientService
{
    private readonly HttpClient _http;
    public WeatherClientService(HttpClient http) => _http = http;

    public async Task<WeatherInfoDto?> GetWeatherAsync(string city)
    {
        try
        {
            return await _http.GetFromJsonAsync<WeatherInfoDto>($"/api/weather?city={Uri.EscapeDataString(city)}");
        }
        catch
        {
            return null;
        }
    }
}
