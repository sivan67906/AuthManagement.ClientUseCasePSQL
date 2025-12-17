using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<PageAccessService> _logger;
    private UserAccessDto? _userAccess;
    private DateTime? _lastLoaded;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10); //  Increased from 5 to 10 minutes

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
        "/two-factor-login",
        "/access-denied"  // Access denied page must be accessible
    };

    // Pages that any authenticated user can access (regardless of role mappings)
    private readonly HashSet<string> _authenticatedUserPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/dashboard",
        "/profile",
        "/access-denied",
        "/change-password",
        "/authenticator-setup",
        "/logout",
        "/notification",
        "/billingplan"
    };

    public PageAccessService(
        RBACService rbacService,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager,
        ILogger<PageAccessService> logger)
    {
        _rbacService = rbacService;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    private async Task<UserAccessDto?> GetUserAccessAsync(bool forceRefresh = false)
    {
        //  Return cached data if still valid
        if (!forceRefresh && _userAccess != null && _lastLoaded.HasValue &&
            DateTime.UtcNow - _lastLoaded.Value < _cacheExpiration)
        {
            _logger.LogDebug("[PAGE_ACCESS] Using cached user access (age: {Age}s)", 
                (DateTime.UtcNow - _lastLoaded.Value).TotalSeconds);
            return _userAccess;
        }

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("[PAGE_ACCESS] User not authenticated");
            return null;
        }

        var email = user.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("[PAGE_ACCESS] No email claim in token");
            return null;
        }

        _logger.LogDebug("[PAGE_ACCESS] Fetching user access for {Email} from API...", email);
        
        try
        {
            var response = await _rbacService.GetUserAccessByEmailAsync(email);
            
            if (response.Success && response.Data != null)
            {
                _userAccess = response.Data;
                _lastLoaded = DateTime.UtcNow;
                _logger.LogInformation("[PAGE_ACCESS]  Successfully loaded user access for {Email}", email);
                return _userAccess;
            }
            else
            {
                _logger.LogWarning("[PAGE_ACCESS] ⚠️ API returned failure: {Message}", response.Message);
                
                //  CRITICAL FIX: If API fails but we have cached data, use it as fallback
                // This prevents "Access Denied" due to temporary API failures
                if (_userAccess != null)
                {
                    _logger.LogWarning("[PAGE_ACCESS] Using stale cached data as fallback (last loaded: {LastLoaded})", 
                        _lastLoaded);
                    return _userAccess;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PAGE_ACCESS] ❌ Exception while fetching user access");
            
            //  CRITICAL FIX: Fall back to cached data on exception
            if (_userAccess != null)
            {
                _logger.LogWarning("[PAGE_ACCESS] Using stale cached data as fallback after exception");
                return _userAccess;
            }
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
            _logger.LogWarning("[PAGE_ACCESS] Invalid page URL provided");
            return new PageAccessResult { HasAccess = false, Reason = "Invalid page URL" };
        }

        pageUrl = NormalizeUrl(pageUrl);
        _logger.LogDebug("[PAGE_ACCESS] Checking access to: {PageUrl}", pageUrl);

        // Check if it's a public page
        if (_publicPages.Contains(pageUrl))
        {
            _logger.LogDebug("[PAGE_ACCESS] {PageUrl} is a public page", pageUrl);
            return new PageAccessResult { HasAccess = true, IsPublicPage = true };
        }

        // Check authentication
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        if (authState.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("[PAGE_ACCESS] User not authenticated for {PageUrl}", pageUrl);
            return new PageAccessResult 
            { 
                HasAccess = false, 
                Reason = "User not authenticated",
                RequiresAuthentication = true
            };
        }

        // Check if it's a page that any authenticated user can access
        if (_authenticatedUserPages.Contains(pageUrl))
        {
            _logger.LogDebug("[PAGE_ACCESS] {PageUrl} is accessible to all authenticated users", pageUrl);
            return new PageAccessResult { HasAccess = true, IsPublicPage = false };
        }

        // Get user access information
        var userAccess = await GetUserAccessAsync();
        if (userAccess == null)
        {
            _logger.LogWarning("[PAGE_ACCESS] ⚠️ Unable to retrieve user access for {PageUrl} - this should not happen with fallback!", pageUrl);
            return new PageAccessResult 
            { 
                HasAccess = false, 
                Reason = "Unable to retrieve user access information" 
            };
        }

        // SuperAdmin has access to all pages
        if (userAccess.Roles.Contains(SystemRoles.SuperAdmin))
        {
            _logger.LogDebug("[PAGE_ACCESS]  SuperAdmin access granted for {PageUrl}", pageUrl);
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
