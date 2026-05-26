using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaxiCompare.Blazor;
using TaxiCompare.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseUrl = builder.HostEnvironment.BaseAddress;

// Регистрируем handler — он автоматически добавляет Bearer токен к каждому запросу
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
});
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<INotificationService, NotificationClientService>();
builder.Services.AddScoped<IWeatherClientService, WeatherClientService>();

var host = builder.Build();

// Ждём пока Gateway проснётся (Render усыпляет сервис после простоя)
await WakeUpGateway(baseUrl);

await host.RunAsync();

static async Task WakeUpGateway(string baseUrl)
{
    using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };
    var maxAttempts = 10;
    for (int i = 0; i < maxAttempts; i++)
    {
        try
        {
            var response = await http.GetAsync("/api/health");
            if (response.IsSuccessStatusCode) return; // Gateway проснулся
        }
        catch { /* ещё спит */ }
        await Task.Delay(3000); // ждём 3 секунды между попытками
    }
    // Если не проснулся за 30 сек — запускаем всё равно
}
