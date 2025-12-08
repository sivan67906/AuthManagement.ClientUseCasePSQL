using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
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

    // 🔥 NEW: Background token refresh timer
    private System.Threading.Timer? _tokenRefreshTimer;
    private const int TokenRefreshCheckIntervalSeconds = 60; // Check every 60 seconds
    private const int TokenRefreshThresholdMinutes = 2; // Refresh when <2 minutes remaining

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

        // 🔥 NEW: Start background token refresh timer
        StartTokenRefreshTimer();
    }

    /// <summary>
    /// 🔥 NEW: Starts a background timer that periodically checks and refreshes expired tokens
    /// </summary>
    private void StartTokenRefreshTimer()
    {
        _tokenRefreshTimer = new System.Threading.Timer(
            async _ => await CheckAndRefreshTokenAsync(),
            null,
            TimeSpan.FromSeconds(TokenRefreshCheckIntervalSeconds),
            TimeSpan.FromSeconds(TokenRefreshCheckIntervalSeconds)
        );
        
        _logger.LogInformation("[AUTH] Token refresh timer started (checks every {Interval}s)", TokenRefreshCheckIntervalSeconds);
    }

    /// <summary>
    /// 🔥 NEW: Proactively checks token expiry and refreshes if needed
    /// This runs in the background every minute to prevent expired tokens
    /// </summary>
    private async Task CheckAndRefreshTokenAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                return; // Not initialized yet
            }

            string? currentToken;
            lock (_stateLock)
            {
                currentToken = _accessToken;
            }

            if (string.IsNullOrWhiteSpace(currentToken))
            {
                return; // No token to refresh
            }

            // Parse token and check expiry
            var token = _tokenHandler.ReadJwtToken(currentToken);
            var timeUntilExpiry = token.ValidTo - DateTime.UtcNow;

            // 🔥 Refresh token if it expires in less than 2 minutes
            if (timeUntilExpiry.TotalMinutes < TokenRefreshThresholdMinutes && timeUntilExpiry.TotalSeconds > 0)
            {
                _logger.LogInformation("[AUTH] Token expires in {Minutes} minutes, proactively refreshing...", 
                    Math.Round(timeUntilExpiry.TotalMinutes, 2));

                var refreshed = await TryRefreshTokenAsync();
                
                if (refreshed)
                {
                    _logger.LogInformation("[AUTH] Proactive token refresh successful");
                }
                else
                {
                    _logger.LogWarning("[AUTH] Proactive token refresh failed");
                }
            }
            else if (timeUntilExpiry.TotalSeconds <= 0)
            {
                _logger.LogWarning("[AUTH] Token already expired, attempting refresh...");
                
                var refreshed = await TryRefreshTokenAsync();
                
                if (!refreshed)
                {
                    // Token expired and refresh failed - clear auth state
                    _logger.LogWarning("[AUTH] Expired token refresh failed, clearing auth state");
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
    /// 🔥 NEW: Centralized token refresh logic
    /// </summary>
    private async Task<bool> TryRefreshTokenAsync()
    {
        // Prevent concurrent refresh attempts
        if (!await _refreshLock.WaitAsync(0))
        {
            _logger.LogDebug("[AUTH] Token refresh already in progress, skipping...");
            return false;
        }

        try
        {
            _logger.LogInformation("[AUTH] Attempting token refresh...");

            var httpClient = _httpClientFactory.CreateClient("AuthApi");
            
            // 🔥 CRITICAL: Call refresh-token endpoint
            // The HttpOnly refresh token cookie will be sent automatically
            var response = await httpClient.PostAsync("/auth/refresh-token", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenRefreshResultDto>>();
                
                if (result?.Success == true && result.Data != null)
                {
                    // Update access token
                    await SetAuthenticationAsync(result.Data.AccessToken);
                    
                    _logger.LogInformation("[AUTH] Token refresh successful, new token expires in {Seconds}s", 
                        result.Data.ExpiresInSeconds);
                    
                    return true;
                }
                else
                {
                    _logger.LogWarning("[AUTH] Token refresh response not successful: {Message}", result?.Message);
                }
            }
            else
            {
                _logger.LogWarning("[AUTH] Token refresh failed with status: {StatusCode}", response.StatusCode);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Token refresh exception");
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Initialize from storage on first call
        if (!_isInitialized && !_isInitializing)
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

                    // 🔥 IMPROVED: Check if token is expired or expiring soon
                    var timeUntilExpiry = token.ValidTo - DateTime.UtcNow;
                    
                    if (timeUntilExpiry.TotalSeconds <= 0)
                    {
                        _logger.LogDebug("[AUTH] Token expired at {ExpiredAt}", token.ValidTo);
                        currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                        _accessToken = null;
                        
                        // 🔥 Trigger background refresh for expired token
                        _ = Task.Run(async () => await TryRefreshTokenAsync());
                    }
                    else if (timeUntilExpiry.TotalMinutes < TokenRefreshThresholdMinutes)
                    {
                        // Token expiring soon - create valid principal but trigger background refresh
                        _logger.LogDebug("[AUTH] Token expiring soon ({Minutes} min), triggering background refresh", 
                            Math.Round(timeUntilExpiry.TotalMinutes, 2));
                        
                        var identity = new ClaimsIdentity(token.Claims, "jwt");
                        currentUser = new ClaimsPrincipal(identity);
                        
                        // 🔥 Trigger background refresh
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

            // 🔥 Check token and trigger refresh if needed
            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                try
                {
                    var token = _tokenHandler.ReadJwtToken(_accessToken);
                    var timeUntilExpiry = token.ValidTo - DateTime.UtcNow;
                    
                    if (timeUntilExpiry.TotalSeconds <= 0)
                    {
                        _logger.LogInformation("[AUTH] Token expired, will attempt refresh");
                        _ = Task.Run(async () => await TryRefreshTokenAsync());
                    }
                    else if (timeUntilExpiry.TotalMinutes < TokenRefreshThresholdMinutes)
                    {
                        _logger.LogInformation("[AUTH] Token expiring soon, will refresh proactively");
                        _ = Task.Run(async () => await TryRefreshTokenAsync());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AUTH] Failed to parse token, will attempt refresh");
                    _ = Task.Run(async () => await TryRefreshTokenAsync());
                }
            }

            _isInitialized = true;
            _isInitializing = false;
            _logger.LogInformation("[AUTH] Initialization complete");
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    // ... (rest of the helper methods remain the same)

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

    // 🔥 NEW: Dispose pattern to stop timer
    public void Dispose()
    {
        _tokenRefreshTimer?.Dispose();
        _logger.LogInformation("[AUTH] Token refresh timer stopped");
    }
}
