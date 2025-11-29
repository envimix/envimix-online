using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace EnvimixWebAPI;

public class LoggingAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler =
        new AuthorizationMiddlewareResultHandler();

    private readonly ILogger<LoggingAuthorizationMiddlewareResultHandler> _logger;

    public LoggingAuthorizationMiddlewareResultHandler(
        ILogger<LoggingAuthorizationMiddlewareResultHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            _logger.LogWarning(
                "Authorization failed for user {User}. Policy: {Policy}",
                context.User?.Identity?.Name,
                string.Join(", ", policy.Requirements.Select(r => r.GetType().Name))
            );
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}