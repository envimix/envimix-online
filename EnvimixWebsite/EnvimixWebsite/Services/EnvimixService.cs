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
    Task RemoveRecordAsync(string mapUid, string login, string carId, int gravity, int laps, int time, CancellationToken cancellationToken = default);
    Task RevertRecordAsync(string mapUid, string login, string carId, int gravity, int laps, int time, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetRegisteredServersAsync(IEnumerable<string> serverLogins, CancellationToken cancellationToken = default);
    Task<EnvimaniaServerSummary[]> GetServersAsync(CancellationToken cancellationToken = default);
    Task<EnvimaniaServerInfo?> GetServerAsync(string serverLogin, int sessionLimit = 10, CancellationToken cancellationToken = default);
    Task<EnvimaniaSessionInfo?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<PlayerInfo?> GetUserAsync(string userLogin, CancellationToken cancellationToken = default);
    Task<RecordInfo?> GetRecordAsync(string mapUid, string car, string userLogin, int time, CancellationToken cancellationToken = default);
    Task<CarInfo?> GetCarAsync(string carId, CancellationToken cancellationToken = default);
    Task<MapDetailsInfo?> GetMapAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<MapRecordsPage?> GetMapRecordsAsync(string mapUid, string? car = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
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

    public async Task RemoveRecordAsync(
        string mapUid,
        string login,
        string carId,
        int gravity,
        int laps,
        int time,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{config["EnvimixApi"]}/envimania/record/remove")
        {
            Content = JsonContent.Create(new { MapUid = mapUid, Login = login, CarId = carId, Gravity = gravity, Laps = laps, Time = time })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RevertRecordAsync(
        string mapUid,
        string login,
        string carId,
        int gravity,
        int laps,
        int time,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{config["EnvimixApi"]}/envimania/record/revert")
        {
            Content = JsonContent.Create(new { MapUid = mapUid, Login = login, CarId = carId, Gravity = gravity, Laps = laps, Time = time })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

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
        int sessionLimit = 10,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{config["EnvimixApi"]}/envimania/servers/{Uri.EscapeDataString(serverLogin)}?sessions={sessionLimit}");
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

    public async Task<EnvimaniaSessionInfo?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{config["EnvimixApi"]}/envimania/sessions/{sessionId}");
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
        return await response.Content.ReadFromJsonAsync<EnvimaniaSessionInfo>(cancellationToken);
    }

    public Task<PlayerInfo?> GetUserAsync(
        string userLogin,
        CancellationToken cancellationToken = default)
        => GetDetailAsync<PlayerInfo>($"players/{Uri.EscapeDataString(userLogin)}", cancellationToken);

    public Task<RecordInfo?> GetRecordAsync(
        string mapUid,
        string car,
        string userLogin,
        int time,
        CancellationToken cancellationToken = default)
        => GetDetailAsync<RecordInfo>(
            $"records/{Uri.EscapeDataString(mapUid)}/{Uri.EscapeDataString(car)}/{Uri.EscapeDataString(userLogin)}/{time}",
            cancellationToken);

    public Task<CarInfo?> GetCarAsync(
        string carId,
        CancellationToken cancellationToken = default)
        => GetDetailAsync<CarInfo>($"cars/{Uri.EscapeDataString(carId)}", cancellationToken);

    public async Task<MapDetailsInfo?> GetMapAsync(
        string mapUid,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{config["EnvimixApi"]}/maps/{Uri.EscapeDataString(mapUid)}");
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
        return await response.Content.ReadFromJsonAsync<MapDetailsInfo>(cancellationToken);
    }

    public Task<MapRecordsPage?> GetMapRecordsAsync(
        string mapUid,
        string? car = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var carQuery = string.IsNullOrWhiteSpace(car)
            ? ""
            : $"&car={Uri.EscapeDataString(car)}";

        return GetDetailAsync<MapRecordsPage>(
            $"maps/{Uri.EscapeDataString(mapUid)}/records?page={page}&pageSize={pageSize}{carQuery}",
            cancellationToken);
    }

    private async Task<T?> GetDetailAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"{config["EnvimixApi"]}/{path}",
            cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
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
    Guid Id,
    string MapUid,
    string MapName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    bool FinishedGracefully);

public sealed record EnvimaniaSessionInfo(
    Guid Id,
    string ServerLogin,
    string MapUid,
    string MapName,
    int MapLaps,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    bool FinishedGracefully,
    bool CanAdminister,
    EnvimaniaSessionRecord[] Records);

public sealed record EnvimaniaSessionRecord(
    string UserLogin,
    string? Nickname,
    string Car,
    int Gravity,
    int Laps,
    int Time,
    int Score,
    int NbRespawns,
    DateTimeOffset DrivenAt,
    bool Removed);

public sealed record PlayerInfo(
    string Login,
    string? Nickname,
    string? Zone,
    int RecordCount,
    RecordInfo[] RecentRecords);

public sealed record CarInfo(
    string Id,
    int RecordCount,
    int PlayerCount,
    RecordInfo[] RecentRecords);

public sealed record RecordInfo(
    string UserLogin,
    string? Nickname,
    string MapUid,
    string MapName,
    int MapLaps,
    string Car,
    int Gravity,
    int Laps,
    int Time,
    int Score,
    int NbRespawns,
    DateTimeOffset DrivenAt,
    Guid? SessionId,
    string? ServerLogin,
    bool Removed);

public sealed record MapDetailsInfo(
    string Name,
    string Uid,
    string Collection,
    int Laps);

public sealed record MapRecordsPage(
    MapRecordCarInfo[] Cars,
    string? Car,
    int Page,
    int PageSize,
    int TotalCount,
    RecordInfo[] Records);

public sealed record MapRecordCarInfo(string Id, int RecordCount);
