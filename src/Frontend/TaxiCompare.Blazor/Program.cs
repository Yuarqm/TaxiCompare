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
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<INotificationService, NotificationClientService>();
builder.Services.AddScoped<IWeatherClientService, WeatherClientService>();

await builder.Build().RunAsync();
