using AuthManagement;
using AuthManagement.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configuration - Read from appsettings.json or use default
var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "https://localhost:27705";
Console.WriteLine("Application starting - Blazor WebAssembly Auth Management System");
Console.WriteLine($"Gateway URL: {gatewayUrl}");

// Add MudBlazor services
builder.Services.AddMudServices();

// Register authentication services
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s =>
    s.GetRequiredService<JwtAuthenticationStateProvider>());

// Register RBAC Service
builder.Services.AddScoped<RBACService>();

// Register Permission Service for page-level permission checking
builder.Services.AddScoped<PermissionService>();

// Register Menu Service for dynamic menu generation
builder.Services.AddScoped<MenuService>();

// Register Navigation Service for hierarchical TreeView menu
builder.Services.AddScoped<NavigationService>();

// Register Page Access Service for URL-level access control
builder.Services.AddScoped<PageAccessService>();

// Register Navigation Guard for additional layer of protection
builder.Services.AddScoped<NavigationGuard>();

// Register Test Entity Service for RBAC testing (Singleton for in-memory data persistence)
builder.Services.AddSingleton<TestEntityService>();

// Register Bootstrap UI Services
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<BootstrapDialogService>();

// Register SweetAlert Service for confirmations and toasts
builder.Services.AddScoped<SweetAlertService>();

// Register Company Service for Company Management
builder.Services.AddScoped<CompanyService>();

//  CRITICAL FIX: Register AuthenticationMessageHandler as Transient
builder.Services.AddScoped<AuthenticationMessageHandler>();

//  CRITICAL FIX: Use "AuthApi" (case-sensitive) consistently
builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
})
.AddHttpMessageHandler<AuthenticationMessageHandler>();

//  CORRECT: Register default HttpClient using IHttpClientFactory
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthApi"));

Console.WriteLine("Cookie support: Browser-managed (automatic)");

// Add authorization services
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();