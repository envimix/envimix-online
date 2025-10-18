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

        var zones = await zoneService.CreateZonesAsync(stoppingToken);

        logger.LogInformation("{count} zones created!", zones.Count());
    }
}