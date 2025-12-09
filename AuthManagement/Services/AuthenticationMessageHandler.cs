using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using AuthManagement.Models;

namespace AuthManagement.Services;

/// <summary>
/// HTTP message handler that automatically attaches JWT bearer token to outgoing requests.
/// Automatically refreshes the access token when it expires (401 response).
///  CRITICAL: Includes browser credentials for cross-origin cookie support.
/// </summary>
public class AuthenticationMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILogger<AuthenticationMessageHandler> _logger;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static bool _isRefreshing = false;
    
    //  Rate limiting for refresh attempts
    private static DateTime _lastRefreshAttempt = DateTime.MinValue;
    private static readonly TimeSpan _minRefreshInterval = TimeSpan.FromSeconds(30);

    public AuthenticationMessageHandler(
        AuthenticationStateProvider authenticationStateProvider,
        ILogger<AuthenticationMessageHandler> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Skip token attachment for auth endpoints to avoid issues
        var path = request.RequestUri?.PathAndQuery ?? "";
        var isAuthEndpoint = path.Contains("/auth/login") || 
                             path.Contains("/auth/register") || 
                             path.Contains("/auth/refresh-token");
        
        //  CRITICAL: Set browser credentials for ALL requests to send cookies cross-origin
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.SetBrowserRequestMode(BrowserRequestMode.Cors);
        
        if (!isAuthEndpoint)
        {
            await AttachTokenAsync(request);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If we get 401 and it's not an auth endpoint, try to refresh the token
        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint)
        {
            _logger.LogInformation("[AuthHandler] Received 401 for {Path}, attempting to refresh token...", path);
            
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            
            if (refreshed)
            {
                _logger.LogInformation("[AuthHandler] Token refreshed successfully, retrying original request");
                
                // Clone the original request (can't reuse the same request)
                var newRequest = await CloneHttpRequestMessageAsync(request);
                
                //  CRITICAL: Set browser credentials on cloned request too
                newRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
                newRequest.SetBrowserRequestMode(BrowserRequestMode.Cors);
                
                await AttachTokenAsync(newRequest);
                
                // Retry the request with new token
                response = await base.SendAsync(newRequest, cancellationToken);
            }
            else
            {
                _logger.LogWarning("[AuthHandler] Token refresh failed, user needs to re-login");
            }
        }

        return response;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        try
        {
            if (_authenticationStateProvider is JwtAuthenticationStateProvider jwtProvider)
            {
                var token = await jwtProvider.GetTokenAsync();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("[AuthHandler] Added Bearer token to request: {Path}", request.RequestUri?.PathAndQuery);
                }
                else
                {
                    _logger.LogDebug("[AuthHandler] No token available for request: {Path}", request.RequestUri?.PathAndQuery);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthHandler] Error attaching token to request");
        }
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        //  Rate limiting - prevent refresh storms
        var timeSinceLastRefresh = DateTime.UtcNow - _lastRefreshAttempt;
        if (timeSinceLastRefresh < _minRefreshInterval)
        {
            _logger.LogDebug("[AuthHandler] Refresh skipped - rate limited (last attempt {Seconds}s ago)", 
                timeSinceLastRefresh.TotalSeconds);
            
            // Still check if we now have a valid token from another refresh
            if (_authenticationStateProvider is JwtAuthenticationStateProvider jwtProvider)
            {
                var token = await jwtProvider.GetTokenAsync();
                return !string.IsNullOrWhiteSpace(token);
            }
            return false;
        }

        // Use lock to prevent multiple simultaneous refresh attempts
        if (_isRefreshing)
        {
            _logger.LogDebug("[AuthHandler] Token refresh already in progress, waiting...");
            await _refreshLock.WaitAsync(cancellationToken);
            _refreshLock.Release();
            
            // After waiting, check if we now have a valid token
            if (_authenticationStateProvider is JwtAuthenticationStateProvider jwtProvider)
            {
                var token = await jwtProvider.GetTokenAsync();
                return !string.IsNullOrWhiteSpace(token);
            }
            return false;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _isRefreshing = true;
            _lastRefreshAttempt = DateTime.UtcNow;
            
            _logger.LogInformation("[AuthHandler] Calling refresh-token endpoint...");
            
            //  CRITICAL: Create request with browser credentials for cross-origin cookies
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh-token");
            refreshRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            refreshRequest.SetBrowserRequestMode(BrowserRequestMode.Cors);
            
            // Send through the base handler (which will include cookies)
            var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);

            _logger.LogDebug("[AuthHandler] Refresh response status: {StatusCode}", refreshResponse.StatusCode);

            if (refreshResponse.IsSuccessStatusCode)
            {
                var result = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<TokenRefreshResultDto>>(cancellationToken: cancellationToken);
                
                if (result?.Success == true && result.Data != null)
                {
                    // Update the auth state with new access token
                    if (_authenticationStateProvider is JwtAuthenticationStateProvider jwtProvider)
                    {
                        await jwtProvider.SetAuthenticationAsync(result.Data.AccessToken);
                        _logger.LogInformation("[AuthHandler]  Token refreshed and auth state updated. New token expires in {Seconds}s", result.Data.ExpiresInSeconds);
                        return true;
                    }
                }
                else
                {
                    _logger.LogWarning("[AuthHandler] Refresh token response was not successful: {Message}", result?.Message);
                }
            }
            else
            {
                var errorContent = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[AuthHandler] Refresh token request failed with status: {StatusCode}, content: {Content}", 
                    refreshResponse.StatusCode, errorContent.Length > 200 ? errorContent[..200] : errorContent);
                
                // Clear auth state on refresh failure - user needs to re-login
                if (_authenticationStateProvider is JwtAuthenticationStateProvider jwtProvider)
                {
                    await jwtProvider.ClearAsync();
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthHandler] Error refreshing token");
            return false;
        }
        finally
        {
            _isRefreshing = false;
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copy the request's content (via stream copy)
        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            // Copy the content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Copy headers (except Authorization which will be re-added)
        foreach (var header in request.Headers)
        {
            if (!header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Copy properties
        foreach (var property in request.Options)
        {
            clone.Options.TryAdd(property.Key, property.Value);
        }

        clone.Version = request.Version;

        return clone;
    }
}
