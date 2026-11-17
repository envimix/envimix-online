namespace EnvimixWebAPI.Services;

public sealed class InitiateZonesBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<InitiateZonesBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Creating zones...");

        using var scope = scopeFactory.CreateScope();
        var zoneService = scope.ServiceProvider.GetRequiredService<IZoneService>();

        try
        {
            var zones = await zoneService.CreateZonesAsync(stoppingToken);

            logger.LogInformation("{count} zones created!", zones.Count());
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            // Zones are only synced from ManiaPlanet here, so a failure must not take the whole API down
            logger.LogError(ex, "Failed to create zones, existing zones will be used.");
        }
    }
}