using EnvimixDiscordBot.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Services;

internal sealed class Scheduler : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly TimeProvider _timeProvider;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<Scheduler> _logger;

    private bool fired = false;

    public Scheduler(
        IServiceProvider provider,
        TimeProvider timeProvider,
        IHostEnvironment env,
        IConfiguration config,
        ILogger<Scheduler> logger)
    {
        _provider = provider;
        _timeProvider = timeProvider;
        _env = env;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("Scheduler is disabled in development mode.");
            return;
        }

        _logger.LogInformation("Starting scheduler...");

        using var periodicTimer = new PeriodicTimer(TimeSpan.Parse(_config.GetRequiredValue("Scheduler:Interval")));

        _logger.LogInformation("Scheduler started, ticking.");

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await TickAsync(stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Ticking...");

        var currentCestDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_timeProvider.GetUtcNow(), "Central European Standard Time");

        if (currentCestDateTime.Day == 1 && currentCestDateTime.Month is 1 or 4 or 7 or 10)
        {
            await TickCampaignDayAsync(currentCestDateTime, cancellationToken);
        }
    }

    private async ValueTask TickCampaignDayAsync(DateTimeOffset currentCestDateTime, CancellationToken cancellationToken)
    {
        if (currentCestDateTime.TimeOfDay < new TimeSpan(17, 1, 0))
        {
            fired = false;
            return;
        }

        if (fired)
        {
            return;
        }

        fired = true;

        await CreateAndAnnounceCampaignAsync(cancellationToken);
    }

    private async Task<bool> CreateAndAnnounceCampaignAsync(CancellationToken cancellationToken)
    {
        await using var scope = _provider.CreateAsyncScope();

        var campaignMaker = scope.ServiceProvider.GetRequiredService<CampaignMaker>();
        var discordReporter = scope.ServiceProvider.GetRequiredService<DiscordReporter>();

        var campaign = await campaignMaker.CreateEnvimixCampaignAsync(submitterId: null, cancellationToken);
        
        if (campaign is null)
        {
            return false;
        }
        
        var announcement = await discordReporter.AnnounceCampaignAsync(campaign, cancellationToken);
        await campaignMaker.UpdateTrackingMessageIdsAsync(campaign, announcement);

        _logger.LogInformation("Seasonal envimix campaign created and announced.");

        return true;
    }
}
