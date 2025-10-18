using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EnvimixDiscordBot;
using EnvimixDiscordBot.Modules;
using EnvimixDiscordBot.Services;
using GBX.NET.Extensions;
using GBX.NET.Hashing;
using GBX.NET.LZO;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddSingleton(TimeProvider.System);

    services.AddDbContext<AppDbContext>(options =>
    {
        var connectionStr = context.Configuration.GetConnectionString("DefaultConnection");
        options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuted));
    });

    // Configure Discord bot
    services.AddSingleton(new DiscordSocketConfig()
    {
        LogLevel = LogSeverity.Verbose
    });

    // Add Discord bot client and Interaction Framework
    services.AddSingleton<DiscordSocketClient>();
    services.AddSingleton<InteractionService>(provider => new(provider.GetRequiredService<DiscordSocketClient>(), new()
    {
        LogLevel = LogSeverity.Verbose,
        //LocalizationManager = new JsonLocalizationManager("Localization", "commands")
    }));

    services.AddLogging(builder =>
    {
        builder.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.AddOtlpExporter();
        });
    });

    // Add startup
    services.AddHostedService<Startup>();
    services.AddHostedService<Scheduler>();

    // Add services
    services.AddSingleton<IDiscordBot, DiscordBot>();
    services.AddSingleton<ILzo, MiniLZO>();
    services.AddSingleton<ICrc32, CRC32>();
    services.AddScoped<CampaignMaker>();
    services.AddScoped<DiscordReporter>();

    services.AddHttpClient<ValidateModule>().AddStandardResilienceHandler();
    services.AddHttpClient<CampaignMaker>().AddStandardResilienceHandler();
    services.AddHttpClient<NadeoLiveServices>().AddStandardResilienceHandler();
    services.AddSingleton<NadeoLiveServices>(
        provider => new(provider.GetRequiredService<HttpClient>(), new()));

    services.AddMemoryCache();

    services.AddOpenTelemetry()
        .WithMetrics(options =>
        {
            options
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter();

            options.AddMeter("System.Net.Http");
        })
        .WithTracing(options =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                options.SetSampler<AlwaysOnSampler>();
            }

            options
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter();
        });
    services.AddMetrics();
});

await builder.Build().RunAsync();
