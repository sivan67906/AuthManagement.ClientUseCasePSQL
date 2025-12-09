using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using AuthManagement.Models;

namespace AuthManagement.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JwtAuthenticationStateProvider> _logger;
    private string? _accessToken;
    private bool _isInitialized;
    private bool _isInitializing;

    private AuthenticationState? _cachedAuthState;
    private DateTime _cacheTimestamp = DateTime.MinValue;
    private readonly TimeSpan _cacheValidity = TimeSpan.FromSeconds(1);
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    //  Background token refresh timer
    private System.Threading.Timer? _tokenRefreshTimer;
    private const int TokenRefreshCheckIntervalSeconds = 60; // Check every 60 seconds
    private const int TokenRefreshThresholdMinutes = 3; // Refresh when <3 minutes remaining (increased from 2)

    //  Rate limiting for refresh attempts
    private DateTime _lastRefreshAttempt = DateTime.MinValue;
    private readonly TimeSpan _minRefreshInterval = TimeSpan.FromSeconds(30);

    // Pending 2FA session storage
    private string? _pendingTwoFactorEmail;
    private string? _pendingTwoFactorToken;
    private string? _pendingTwoFactorType;

    // Storage keys
    private const string AccessTokenStorageKey = "accessToken";
    private const string Pending2FAEmailKey = "pending_2fa_email";
    private const string Pending2FATokenKey = "pending_2fa_token";
    private const string Pending2FATypeKey = "pending_2fa_type";

    public JwtAuthenticationStateProvider(
        IJSRuntime jsRuntime,
        IHttpClientFactory httpClientFactory,
        ILogger<JwtAuthenticationStateProvider> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        //  Start background token refresh timer
        StartTokenRefreshTimer();
    }

    /// <summary>
    ///  Starts a background timer that periodically checks and refreshes expired tokens
    /// </summary>
    private void StartTokenRefreshTimer()
    {
        _tokenRefreshTimer = new System.Threading.Timer(
            async _ => await SafeCheckAndRefreshTokenAsync(),
            null,
            TimeSpan.FromSeconds(TokenRefreshCheckIntervalSeconds),
            TimeSpan.FromSeconds(TokenRefreshCheckIntervalSeconds)
        );
        
        _logger.LogInformation("[AUTH] Token refresh timer started (checks every {Interval}s, threshold {Threshold}m)", 
            TokenRefreshCheckIntervalSeconds, TokenRefreshThresholdMinutes);
    }

    /// <summary>
    ///  Safe wrapper for async timer callback with error handling
    /// </summary>
    private async Task SafeCheckAndRefreshTokenAsync()
    {
        try
        {
            await CheckAndRefreshTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Unhandled error in background token refresh check");
        }
    }

    /// <summary>
    ///  Proactively checks token expiry and refreshes if needed
    /// This runs in the background every minute to prevent expired tokens
    /// </summary>
    private async Task CheckAndRefreshTokenAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                _logger.LogDebug("[AUTH] Background refresh skipped - not initialized yet");
                return;
            }

            string? currentToken;
            lock (_stateLock)
            {
                currentToken = _accessToken;
            }

            if (string.IsNullOrWhiteSpace(currentToken))
            {
                _logger.LogDebug("[AUTH] Background refresh skipped - no token");
                return;
            }

            // Parse token and check expiry
            var token = _tokenHandler.ReadJwtToken(currentToken);
            var timeUntilExpiry = token.ValidTo - DateTime.UtcNow;

            _logger.LogDebug("[AUTH] Background check - token expires in {Minutes:F2} minutes", timeUntilExpiry.TotalMinutes);

            //  Refresh token if it expires in less than threshold minutes
            if (timeUntilExpiry.TotalMinutes < TokenRefreshThresholdMinutes && timeUntilExpiry.TotalSeconds > 0)
            {
                _logger.LogInformation("[AUTH] Token expires in {Minutes:F2} minutes (threshold={Threshold}m), proactively refreshing...", 
                    timeUntilExpiry.TotalMinutes, TokenRefreshThresholdMinutes);

                var refreshed = await TryRefreshTokenAsync();
                
                if (refreshed)
                {
                    _logger.LogInformation("[AUTH]  Proactive token refresh successful at {Time}", DateTime.Now.ToString("HH:mm:ss"));
                }
                else
                {
                    _logger.LogWarning("[AUTH] ⚠️ Proactive token refresh failed");
                }
            }
            else if (timeUntilExpiry.TotalSeconds <= 0)
            {
                _logger.LogWarning("[AUTH] ⚠️ Token already expired at {ExpiredAt}, attempting refresh...", token.ValidTo);
                
                var refreshed = await TryRefreshTokenAsync();
                
                if (!refreshed)
                {
                    // Token expired and refresh failed - clear auth state
                    _logger.LogWarning("[AUTH] ❌ Expired token refresh failed, clearing auth state");
                    await ClearAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Error in background token refresh check");
        }
    }

    /// <summary>
    ///  Centralized token refresh logic with proper browser credentials
    /// </summary>
    private async Task<bool> TryRefreshTokenAsync()
    {
        //  Rate limiting - prevent refresh storms
        var timeSinceLastRefresh = DateTime.UtcNow - _lastRefreshAttempt;
        if (timeSinceLastRefresh < _minRefreshInterval)
        {
            _logger.LogDebug("[AUTH] Refresh skipped - rate limited (last attempt {Seconds}s ago)", 
                timeSinceLastRefresh.TotalSeconds);
            return false;
        }

        // Prevent concurrent refresh attempts
        if (!await _refreshLock.WaitAsync(0))
        {
            _logger.LogDebug("[AUTH] Token refresh already in progress, skipping...");
            return false;
        }

        try
        {
            _lastRefreshAttempt = DateTime.UtcNow;
            _logger.LogInformation("[AUTH] 🔄 Attempting token refresh at {Time}...", DateTime.Now.ToString("HH:mm:ss"));

            var httpClient = _httpClientFactory.CreateClient("AuthApi");
            
            //  CRITICAL FIX: Create request with browser credentials for cross-origin cookies
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh-token");
            
            //  CRITICAL: This is required for Blazor WASM to send HttpOnly cookies cross-origin
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.SetBrowserRequestMode(BrowserRequestMode.Cors);
            
            var response = await httpClient.SendAsync(request);

            _logger.LogDebug("[AUTH] Refresh response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenRefreshResultDto>>();
                
                if (result?.Success == true && result.Data != null)
                {
                    // Update access token
                    await SetAuthenticationAsync(result.Data.AccessToken);
                    
                    _logger.LogInformation("[AUTH]  Token refresh successful, new token expires in {Seconds}s ({Minutes:F1}m)", 
                        result.Data.ExpiresInSeconds, result.Data.ExpiresInSeconds / 60.0);
                    
                    return true;
                }
                else
                {
                    _logger.LogWarning("[AUTH] ⚠️ Token refresh response not successful: {Message}", result?.Message);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[AUTH] ⚠️ Token refresh failed with status: {StatusCode}, content: {Content}", 
                    response.StatusCode, errorContent.Length > 200 ? errorContent[..200] : errorContent);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] ❌ Token refresh exception");
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    ///  Public method to force a token refresh (can be called from UI)
    /// </summary>
    public async Task<bool> ForceRefreshAsync()
    {
        _logger.LogInformation("[AUTH] Force refresh requested");
        _lastRefreshAttempt = DateTime.MinValue; // Reset rate limiting
        return await TryRefreshTokenAsync();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        //  CRITICAL FIX: Always call InitializeFromStorageAsync if not initialized
        // The method has internal locking - concurrent callers will WAIT (not skip)
        // This fixes the race condition where Call B would skip init and return unauthenticated
        if (!_isInitialized)
        {
            await InitializeFromStorageAsync();
        }

        lock (_stateLock)
        {
            // Return cached state if still valid
            if (_cachedAuthState != null &&
                (DateTime.UtcNow - _cacheTimestamp) < _cacheValidity)
            {
                return _cachedAuthState;
            }

            ClaimsPrincipal currentUser;

            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
            else
            {
                try
                {
                    var token = _tokenHandler.ReadJwtToken(_accessToken);

                    // Check if token is expired or expiring soon
                    var timeUntilExpiry = token.ValidTo - DateTime.UtcNow;
                    
                    if (timeUntilExpiry.TotalSeconds <= 0)
                    {
                        _logger.LogDebug("[AUTH] Token expired at {ExpiredAt}", token.ValidTo);
                        currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                        _accessToken = null;
                        
                        // Trigger background refresh for expired token
                        _ = Task.Run(async () => await TryRefreshTokenAsync());
                    }
                    else if (timeUntilExpiry.TotalMinutes < TokenRefreshThresholdMinutes)
                    {
                        // Token expiring soon - create valid principal but trigger background refresh
                        _logger.LogDebug("[AUTH] Token expiring soon ({Minutes:F2}m), triggering background refresh", 
                            timeUntilExpiry.TotalMinutes);
                        
                        var identity = new ClaimsIdentity(token.Claims, "jwt");
                        currentUser = new ClaimsPrincipal(identity);
                        
                        // Trigger background refresh
                        _ = Task.Run(async () => await TryRefreshTokenAsync());
                    }
                    else
                    {
                        // Token is valid
                        var identity = new ClaimsIdentity(token.Claims, "jwt");
                        currentUser = new ClaimsPrincipal(identity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTH] Token parsing failed");
                    currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }

            _cachedAuthState = new AuthenticationState(currentUser);
            _cacheTimestamp = DateTime.UtcNow;

            return _cachedAuthState;
        }
    }

    private async Task InitializeFromStorageAsync()
    {
        await _initializationLock.WaitAsync();
        
        try
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitializing = true;
            _logger.LogInformation("[AUTH] Initializing authentication state from storage...");

            // Load access token from localStorage
            _accessToken = await GetFromLocalStorageAsync(AccessTokenStorageKey);

            // Load pending 2FA session
            _pendingTwoFactorEmail = await GetFromLocalStorageAsync(Pending2FAEmailKey);
            _pendingTwoFactorToken = await GetFromLocalStorageAsync(Pending2FATokenKey);
            _pendingTwoFactorType = await GetFromLocalStorageAsync(Pending2FATypeKey);

            _logger.LogInformation("[AUTH] Token from localStorage: {HasToken}", !string.IsNullOrWhiteSpace(_accessToken));

            //  CRITICAL FIX: Determine if we need to refresh and AWAIT the result
            bool needsRefresh = false;
            bool tokenExpired = false;
            
            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                try
                {
                    var token = _tokenHandler.ReadJwtToken(_accessToken);
                    var timeUntilExpiry = token.ValidTo - DateTime.UtcNow;
                    
                    _logger.LogInformation("[AUTH] Token expires at {ExpiresAt} (in {Minutes:F2} minutes)", 
                        token.ValidTo, timeUntilExpiry.TotalMinutes);
                    
                    if (timeUntilExpiry.TotalSeconds <= 0)
                    {
                        _logger.LogInformation("[AUTH] Token expired, will attempt refresh");
                        needsRefresh = true;
                        tokenExpired = true;
                    }
                    else if (timeUntilExpiry.TotalMinutes < TokenRefreshThresholdMinutes)
                    {
                        _logger.LogInformation("[AUTH] Token expiring soon, will refresh proactively");
                        needsRefresh = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AUTH] Failed to parse token, will attempt refresh");
                    needsRefresh = true;
                    tokenExpired = true;
                }
            }
            else
            {
                //  No token in localStorage - try to refresh using HttpOnly refresh token cookie
                _logger.LogInformation("[AUTH] No token in localStorage, attempting refresh with cookie...");
                needsRefresh = true;
                tokenExpired = true;
            }

            //  CRITICAL: AWAIT the refresh attempt on initialization (not fire-and-forget)
            if (needsRefresh)
            {
                _logger.LogInformation("[AUTH] 🔄 Attempting token refresh during initialization...");
                
                var refreshSuccess = await TryRefreshTokenAsync();
                
                if (refreshSuccess)
                {
                    _logger.LogInformation("[AUTH]  Token refresh successful during initialization");
                }
                else
                {
                    _logger.LogWarning("[AUTH] ⚠️ Token refresh failed during initialization");
                    
                    // If token was expired or missing and refresh failed, clear everything
                    if (tokenExpired)
                    {
                        _logger.LogInformation("[AUTH] Clearing expired/invalid token from localStorage");
                        _accessToken = null;
                        await RemoveFromLocalStorageAsync(AccessTokenStorageKey);
                    }
                }
            }

            _isInitialized = true;
            _isInitializing = false;
            _logger.LogInformation("[AUTH] Initialization complete. Has valid token: {HasToken}", !string.IsNullOrWhiteSpace(_accessToken));
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task<string?> GetFromLocalStorageAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    private async Task SetInLocalStorageAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Failed to set {Key} in localStorage", key);
        }
    }

    private async Task RemoveFromLocalStorageAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Failed to remove {Key} from localStorage", key);
        }
    }

    public async Task RefreshAuthenticationStateAsync()
    {
        try
        {
            lock (_stateLock)
            {
                _cachedAuthState = null;
            }

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            _logger.LogInformation("[AUTH] Authentication state refreshed and notification sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Failed to refresh authentication state");
        }
    }

    public async Task SetAuthenticationAsync(string? token)
    {
        lock (_stateLock)
        {
            _logger.LogInformation("[AUTH] Setting authentication with token: {HasToken}", !string.IsNullOrWhiteSpace(token));
            _accessToken = token;
            _cachedAuthState = null;
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            await SetInLocalStorageAsync(AccessTokenStorageKey, token);
            _logger.LogInformation("[AUTH] Access token stored in localStorage");
        }
        else
        {
            await RemoveFromLocalStorageAsync(AccessTokenStorageKey);
            _logger.LogInformation("[AUTH] Access token removed from localStorage");
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        _logger.LogInformation("[AUTH] Authentication set and state notification sent");
    }

    public async Task ClearAsync()
    {
        lock (_stateLock)
        {
            _logger.LogInformation("[AUTH] Clearing authentication state");
            _accessToken = null;
            _cachedAuthState = null;
            _pendingTwoFactorEmail = null;
            _pendingTwoFactorToken = null;
            _pendingTwoFactorType = null;
        }

        await RemoveFromLocalStorageAsync(AccessTokenStorageKey);
        await RemoveFromLocalStorageAsync(Pending2FAEmailKey);
        await RemoveFromLocalStorageAsync(Pending2FATokenKey);
        await RemoveFromLocalStorageAsync(Pending2FATypeKey);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        _logger.LogInformation("[AUTH] Authentication cleared");
    }

    public async Task<string?> GetTokenAsync()
    {
        string? currentToken;
        lock (_stateLock)
        {
            currentToken = _accessToken;
        }
        
        if (!string.IsNullOrWhiteSpace(currentToken))
        {
            return currentToken;
        }

        var storageToken = await GetFromLocalStorageAsync(AccessTokenStorageKey);
        if (!string.IsNullOrWhiteSpace(storageToken))
        {
            lock (_stateLock)
            {
                _accessToken = storageToken;
            }
            return storageToken;
        }

        if (!_isInitialized)
        {
            await InitializeFromStorageAsync();
        }

        lock (_stateLock)
        {
            return _accessToken;
        }
    }

    public void InvalidateCache()
    {
        lock (_stateLock)
        {
            _cachedAuthState = null;
        }
    }

    // 2FA methods
    public async Task SetPendingTwoFactorAsync(string email, string twoFactorToken, string twoFactorType)
    {
        lock (_stateLock)
        {
            _logger.LogInformation("[AUTH] Setting pending 2FA: Email={Email}, Type={Type}", email, twoFactorType);
            _pendingTwoFactorEmail = email;
            _pendingTwoFactorToken = twoFactorToken;
            _pendingTwoFactorType = twoFactorType;
        }

        await SetInLocalStorageAsync(Pending2FAEmailKey, email);
        await SetInLocalStorageAsync(Pending2FATokenKey, twoFactorToken);
        await SetInLocalStorageAsync(Pending2FATypeKey, twoFactorType);
    }

    public async Task<(string? Email, string? Token, string? Type)> GetPendingTwoFactorAsync()
    {
        if (!_isInitialized)
        {
            await InitializeFromStorageAsync();
        }

        lock (_stateLock)
        {
            return (_pendingTwoFactorEmail, _pendingTwoFactorToken, _pendingTwoFactorType);
        }
    }

    public async Task ClearPendingTwoFactorAsync()
    {
        lock (_stateLock)
        {
            _logger.LogInformation("[AUTH] Clearing pending 2FA session");
            _pendingTwoFactorEmail = null;
            _pendingTwoFactorToken = null;
            _pendingTwoFactorType = null;
        }

        await RemoveFromLocalStorageAsync(Pending2FAEmailKey);
        await RemoveFromLocalStorageAsync(Pending2FATokenKey);
        await RemoveFromLocalStorageAsync(Pending2FATypeKey);
    }

    public async Task<bool> HasPendingTwoFactorAsync()
    {
        if (!_isInitialized)
        {
            await InitializeFromStorageAsync();
        }

        lock (_stateLock)
        {
            return !string.IsNullOrEmpty(_pendingTwoFactorEmail) &&
                   !string.IsNullOrEmpty(_pendingTwoFactorToken);
        }
    }

    // Legacy methods
    public Task SetTokenAsync(string? token) => SetAuthenticationAsync(token);

    public Task SetTwoFactorVerifiedAsync(bool verified)
    {
        _logger.LogInformation("[AUTH] SetTwoFactorVerifiedAsync called (deprecated): {Verified}", verified);
        return Task.CompletedTask;
    }

    public Task<bool> IsTwoFactorVerifiedAsync()
    {
        lock (_stateLock)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(_accessToken));
        }
    }

    //  Dispose pattern to stop timer and release resources
    public void Dispose()
    {
        _tokenRefreshTimer?.Dispose();
        _initializationLock?.Dispose();
        _refreshLock?.Dispose();
        _logger.LogInformation("[AUTH] Token refresh timer and resources disposed");
    }
}

/// <summary>
/// DTO for refresh token response
/// </summary>
public class TokenRefreshResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public string NewRefreshToken { get; set; } = string.Empty;
}
