using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using AuthManagement.Services;

namespace AuthManagement.Services;

/// <summary>
/// Navigation guard that monitors all navigation events and enforces page access control
/// This provides an additional layer of protection by checking access on every navigation
/// </summary>
public class NavigationGuard : IDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly PageAccessService _pageAccessService;
    private bool _isNavigating = false;

    public NavigationGuard(
        NavigationManager navigationManager,
        PageAccessService pageAccessService)
    {
        _navigationManager = navigationManager;
        _pageAccessService = pageAccessService;

        // Subscribe to location changed events
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // Prevent infinite loops
        if (_isNavigating) return;

        try
        {
            _isNavigating = true;

            var uri = new Uri(e.Location);
            var path = uri.LocalPath;

            // Check if user has access to the new location
            var result = await _pageAccessService.HasAccessToPageAsync(path);

            if (!result.HasAccess)
            {
                // Cancel navigation and redirect appropriately
                if (result.RequiresAuthentication)
                {
                    _navigationManager.NavigateTo("/login", true);
                }
                else
                {
                    _navigationManager.NavigateTo("/access-denied", true);
                }
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
