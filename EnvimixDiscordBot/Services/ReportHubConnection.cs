using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Services;

public interface IReportHubConnection
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

internal sealed class ReportHubConnection : IReportHubConnection
{
    private readonly HubConnection connection;
    private readonly ILogger<ReportHubConnection> logger;

    public ReportHubConnection(IConfiguration config, ILogger<ReportHubConnection> logger)
    {
        this.logger = logger;

        var envimixWebApi = config["EnvimixWebAPI"];

        if (string.IsNullOrWhiteSpace(envimixWebApi))
        {
            throw new Exception("EnvimixWebAPI is missing, cannot connect to the API.");
        }

        connection = new HubConnectionBuilder()
            .WithUrl($"{envimixWebApi}/ReportHub")
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting...");

        await connection.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Disconnecting...");

        await connection.StopAsync(cancellationToken);
    }
}
