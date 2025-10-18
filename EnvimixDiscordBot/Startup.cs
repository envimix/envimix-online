using GBX.NET.Extensions;
using GBX.NET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ManiaAPI.NadeoAPI;
using Microsoft.Extensions.Configuration;
using EnvimixDiscordBot.Extensions;
using EnvimixDiscordBot.Services;

namespace EnvimixDiscordBot;

internal sealed class Startup : IHostedService
{
    private readonly IServiceProvider _provider;
    private readonly IDiscordBot _bot;
    private readonly ILzo _lzo;
    private readonly ICrc32 _crc32;
    private readonly NadeoLiveServices _nls;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<Startup> _logger;

    public Startup(
        IServiceProvider provider,
        IDiscordBot bot,
        ILzo lzo,
        ICrc32 crc32,
        NadeoLiveServices nls,
        IConfiguration config,
        IHostEnvironment environment,
        ILogger<Startup> logger)
    {
        _provider = provider;
        _bot = bot;
        _lzo = lzo;
        _crc32 = crc32;
        _nls = nls;
        _config = config;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Gbx.LZO = _lzo;
        Gbx.CRC32 = _crc32;

        _logger.LogInformation("Syncing database...");

        await using var scope = _provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;

        if (_environment.IsDevelopment() && db.IsRelational())
        {
            await db.MigrateAsync(cancellationToken);
        }

        _logger.LogInformation("Starting bot...");

        var dedicatedServerLogin = _config.GetRequiredValue("DedicatedServer:TM2020:Login");
        var dedicatedServerPassword = _config.GetRequiredValue("DedicatedServer:TM2020:Password");

        await Task.WhenAll(
            _bot.StartAsync(),
            _nls.AuthorizeAsync(
                dedicatedServerLogin,
                dedicatedServerPassword,
                AuthorizationMethod.DedicatedServer,
                cancellationToken)
            );

        /*try
        {
            await reportHubConnection.StartAsync(cancellationToken);

            logger.LogInformation("Connected to ReportHub!");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Cannot connect to ReportHub!");
        }*/
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.StopAsync();
    }
}
