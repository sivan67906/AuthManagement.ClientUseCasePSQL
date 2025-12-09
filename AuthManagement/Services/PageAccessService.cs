using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using AuthManagement.Models;
using AuthManagement.Constants;

namespace AuthManagement.Services;

/// <summary>
/// Service to check if a user has access to specific pages based on their role mappings
/// </summary>
public class PageAccessService
{
    private readonly RBACService _rbacService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;
    private UserAccessDto? _userAccess;
    private DateTime? _lastLoaded;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    // Public pages that don't require authorization
    private readonly HashSet<string> _publicPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/login",
        "/register",
        "/forgot-password",
        "/reset-password",
        "/confirm-email",
        "/verify-mail",
        "/send-confirmation-email",
        "/two-factor-login"
    };

    public PageAccessService(
        RBACService rbacService,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager)
    {
        _rbacService = rbacService;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
    }

    private async Task<UserAccessDto?> GetUserAccessAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _userAccess != null && _lastLoaded.HasValue &&
            DateTime.UtcNow - _lastLoaded.Value < _cacheExpiration)
        {
            return _userAccess;
        }

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var email = user.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var response = await _rbacService.GetUserAccessByEmailAsync(email);
        if (response.Success && response.Data != null)
        {
            _userAccess = response.Data;
            _lastLoaded = DateTime.UtcNow;
            return _userAccess;
        }

        return null;
    }

    /// <summary>
    /// Check if the user has access to a specific page URL
    /// </summary>
    public async Task<PageAccessResult> HasAccessToPageAsync(string pageUrl)
    {
        // Normalize the URL
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            return new PageAccessResult { HasAccess = false, Reason = "Invalid page URL" };
        }

        pageUrl = NormalizeUrl(pageUrl);

        // Check if it's a public page
        if (_publicPages.Contains(pageUrl))
        {
            return new PageAccessResult { HasAccess = true, IsPublicPage = true };
        }

        // Check authentication
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        if (authState.User?.Identity?.IsAuthenticated != true)
        {
            return new PageAccessResult 
            { 
                HasAccess = false, 
                Reason = "User not authenticated",
                RequiresAuthentication = true
            };
        }

        // Get user access information
        var userAccess = await GetUserAccessAsync();
        if (userAccess == null)
        {
            return new PageAccessResult 
            { 
                HasAccess = false, 
                Reason = "Unable to retrieve user access information" 
            };
        }

        // SuperAdmin has access to all pages
        if (userAccess.Roles.Contains(SystemRoles.SuperAdmin))
        {
            return new PageAccessResult { HasAccess = true, IsSuperAdmin = true };
        }

        // Check if user has access to this specific page
        var hasPageAccess = userAccess.PageAccess.Any(p => 
            NormalizeUrl(p.PageUrl).Equals(pageUrl, StringComparison.OrdinalIgnoreCase));

        if (!hasPageAccess)
        {
            return new PageAccessResult 
            { 
                HasAccess = false, 
                Reason = $"User does not have access to page: {pageUrl}",
                UserRoles = userAccess.Roles,
                UserEmail = userAccess.Email
            };
        }

        // Check if user has at least View permission on this page
        var pageName = GetPageNameFromUrl(userAccess, pageUrl);
        if (!string.IsNullOrEmpty(pageName))
        {
            var permissions = userAccess.GetPermissionsForPage(pageName);
            if (permissions.Any(p => p.Equals("View", StringComparison.OrdinalIgnoreCase)))
            {
                return new PageAccessResult 
                { 
                    HasAccess = true, 
                    PageName = pageName,
                    Permissions = permissions
                };
            }
        }

        return new PageAccessResult 
        { 
            HasAccess = false, 
            Reason = "User does not have View permission for this page" 
        };
    }

    /// <summary>
    /// Check if current navigation URL is accessible
    /// </summary>
    public async Task<PageAccessResult> CheckCurrentPageAccessAsync()
    {
        var currentUrl = new Uri(_navigationManager.Uri).LocalPath;
        return await HasAccessToPageAsync(currentUrl);
    }

    /// <summary>
    /// Navigate to access denied page if user doesn't have access
    /// </summary>
    public async Task<bool> EnforcePageAccessAsync()
    {
        var result = await CheckCurrentPageAccessAsync();
        
        if (!result.HasAccess)
        {
            if (result.RequiresAuthentication)
            {
                _navigationManager.NavigateTo("/login");
            }
            else
            {
                _navigationManager.NavigateTo("/access-denied");
            }
            return false;
        }

        return true;
    }

    private string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "/";

        // Remove trailing slash except for root
        url = url.TrimEnd('/');
        if (string.IsNullOrEmpty(url))
            return "/";

        // Ensure it starts with /
        if (!url.StartsWith("/"))
            url = "/" + url;

        return url.ToLowerInvariant();
    }

    private string? GetPageNameFromUrl(UserAccessDto userAccess, string pageUrl)
    {
        var page = userAccess.PageAccess.FirstOrDefault(p => 
            NormalizeUrl(p.PageUrl).Equals(NormalizeUrl(pageUrl), StringComparison.OrdinalIgnoreCase));
        return page?.PageName;
    }

    public void ClearCache()
    {
        _userAccess = null;
        _lastLoaded = null;
    }
}

public class PageAccessResult
{
    public bool HasAccess { get; set; }
    public string? Reason { get; set; }
    public bool IsPublicPage { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool RequiresAuthentication { get; set; }
    public string? PageName { get; set; }
    public List<string>? Permissions { get; set; }
    public List<string>? UserRoles { get; set; }
    public string? UserEmail { get; set; }
}
