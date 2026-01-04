using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace EnvimixWebsite.Services;

public interface IIdentityService
{
    Task<UserDto?> GetCurrentUserAsync(ClaimsPrincipal user);
}

public class IdentityService : IIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly HybridCache _cache;
    private readonly ILogger<IdentityService> _logger;
    private readonly IConfiguration config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityService(
        HttpClient httpClient,
        HybridCache cache,
        ILogger<IdentityService> logger,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        this.config = config;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserDto?> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext not available");
            return null;
        }

        // Get access token from authentication properties
        var accessToken = await httpContext.GetTokenAsync("access_token");
        
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No access token found in authentication properties");
            return null;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user ID found in claims");
            return null;
        }

        return await _cache.GetOrCreateAsync(
            $"identity_user_{userId}",
            async cancellationToken =>
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"{config["IdentityManager"]}/api/me");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var userInfo = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
                        _logger.LogDebug("Successfully fetched user info for {UserId}", userId);
                        return userInfo;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to fetch user info. Status: {StatusCode}", response.StatusCode);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching user info from identity service");
                    return null;
                }
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(10)
            });
    }
}

public sealed class UserDto
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public string? DisplayName { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; }
    public required Dictionary<string, UserProviderDto> Providers { get; set; }
    public UserData? Data { get; set; }
}

public sealed class UserProviderDto
{
    public required string Id { get; set; }
    public string? Name { get; set; }
}

public sealed class UserData
{
    [JsonPropertyName(nameof(ManiaPlanet))]
    public ManiaPlanetUserData? ManiaPlanet { get; set; }
}

public sealed class ManiaPlanetUserData
{
    public ImmutableList<ManiaPlanetDedicatedAccountData> DedicatedAccounts { get; set; } = [];
}

public sealed class ManiaPlanetDedicatedAccountData
{
    public required string Login { get; set; }
    public required long? LastUsedAt { get; set; }
}
