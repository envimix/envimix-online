using Microsoft.AspNetCore.Authentication;

namespace EnvimixWebsite.Services;

public interface IEnvimixService
{
    Task RegisterServerAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task DeleteServerAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task WipeServerAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task DeleteServerRecordsAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task DeleteServerRatingsAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task BanServerAsync(string serverLogin, string reason, CancellationToken cancellationToken = default);
    Task UnbanServerAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetRegisteredServersAsync(IEnumerable<string> serverLogins, CancellationToken cancellationToken = default);
    Task<EnvimaniaServerSummary[]> GetServersAsync(CancellationToken cancellationToken = default);
    Task<EnvimaniaServerInfo?> GetServerAsync(string serverLogin, CancellationToken cancellationToken = default);
}

public sealed class EnvimixService(
    HttpClient httpClient,
    IConfiguration config,
    IHttpContextAccessor httpContextAccessor,
    ILogger<EnvimixService> logger) : IEnvimixService
{
    public async Task RegisterServerAsync(string serverLogin, CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var accessToken = httpContext is null ? null : await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogWarning("No access token available while registering server {ServerLogin}", serverLogin);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config["EnvimixApi"]}/envimania/register")
        {
            Content = JsonContent.Create(new { ServerLogin = serverLogin })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.UnprocessableEntity)
        {
            logger.LogWarning("Failed to register server {ServerLogin}. Status: {StatusCode}", serverLogin, response.StatusCode);
        }
    }

    public async Task DeleteServerAsync(string serverLogin, CancellationToken cancellationToken = default)
        => await SendServerCommandAsync(HttpMethod.Delete, serverLogin, null, cancellationToken);

    public async Task WipeServerAsync(string serverLogin, CancellationToken cancellationToken = default)
        => await SendServerCommandAsync(HttpMethod.Delete, serverLogin, "wipe", cancellationToken);

    public async Task DeleteServerRecordsAsync(string serverLogin, CancellationToken cancellationToken = default)
        => await SendServerCommandAsync(HttpMethod.Delete, serverLogin, "records", cancellationToken);

    public async Task DeleteServerRatingsAsync(string serverLogin, CancellationToken cancellationToken = default)
        => await SendServerCommandAsync(HttpMethod.Delete, serverLogin, "ratings", cancellationToken);

    public async Task BanServerAsync(
        string serverLogin,
        string reason,
        CancellationToken cancellationToken = default)
        => await SendServerCommandAsync(HttpMethod.Post, serverLogin, "ban", cancellationToken, new { Reason = reason });

    public async Task UnbanServerAsync(string serverLogin, CancellationToken cancellationToken = default)
        => await SendServerCommandAsync(HttpMethod.Post, serverLogin, "unban", cancellationToken);

    private async Task SendServerCommandAsync(
        HttpMethod method,
        string serverLogin,
        string? action,
        CancellationToken cancellationToken,
        object? content = null)
    {
        var accessToken = await GetAccessTokenAsync();
        var actionPath = string.IsNullOrEmpty(action) ? "" : $"/{action}";

        using var request = new HttpRequestMessage(method,
            $"{config["EnvimixApi"]}/envimania/servers/{Uri.EscapeDataString(serverLogin)}{actionPath}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<HashSet<string>> GetRegisteredServersAsync(
        IEnumerable<string> serverLogins,
        CancellationToken cancellationToken = default)
    {
        var query = string.Join("&", serverLogins.Select(x => $"serverLogin={Uri.EscapeDataString(x)}"));
        if (string.IsNullOrEmpty(query))
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }

        var registered = await httpClient.GetFromJsonAsync<string[]>(
            $"{config["EnvimixApi"]}/envimania/registered?{query}",
            cancellationToken) ?? [];

        return new HashSet<string>(registered, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<EnvimaniaServerSummary[]> GetServersAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{config["EnvimixApi"]}/envimania/servers");
        var accessToken = await GetAccessTokenAsync(required: false);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EnvimaniaServerSummary[]>(cancellationToken) ?? [];
    }

    public async Task<EnvimaniaServerInfo?> GetServerAsync(
        string serverLogin,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{config["EnvimixApi"]}/envimania/servers/{Uri.EscapeDataString(serverLogin)}");
        var accessToken = await GetAccessTokenAsync(required: false);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EnvimaniaServerInfo>(cancellationToken);
    }

    private async Task<string?> GetAccessTokenAsync(bool required = true)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var accessToken = httpContext is null ? null : await httpContext.GetTokenAsync("access_token");
        if (required && string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("No access token available for server management.");
        }

        return accessToken;
    }
}

public sealed record EnvimaniaServerSummary(
    string ServerLogin,
    int SessionCount,
    DateTimeOffset? LastSeenAt,
    bool IsHidden,
    bool IsBanned);

public sealed record EnvimaniaServerInfo(
    string ServerLogin,
    int SessionCount,
    DateTimeOffset? LastSeenAt,
    EnvimaniaServerSession[] RecentSessions,
    bool IsHidden,
    bool IsBanned,
    bool CanDelete,
    bool CanAdminister);

public sealed record EnvimaniaServerSession(
    string MapUid,
    string MapName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);
