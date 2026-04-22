
using Discord;
using Discord.Webhook;
using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using System.Threading.Channels;
using TmEssentials;

namespace EnvimixWebAPI.Services;

public sealed class WorldRecordWebhookProcessor : BackgroundService
{
    private readonly Channel<WorldRecordWebhookDispatch> webhookChannel;
    private readonly IConfiguration config;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<WorldRecordWebhookProcessor> logger;

    public WorldRecordWebhookProcessor(Channel<WorldRecordWebhookDispatch> webhookChannel, IConfiguration config, IServiceScopeFactory scopeFactory, ILogger<WorldRecordWebhookProcessor> logger)
    {
        this.webhookChannel = webhookChannel;
        this.config = config;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var webhook in webhookChannel.Reader.ReadAllAsync(stoppingToken))
        {
            if (webhook.PrevRecord is null)
            {
                logger.LogInformation("Skipping validation for world record webhook for map {MapName}", webhook.NewRecord.Map.Name);
                continue;
            }

            try
            {
                using var client = new DiscordWebhookClient(config["DiscordRecordWebhook"]);

                var envEmote = ValidationWebhookProcessor.GetEnvEmote(webhook.NewRecord.Map);
                var carEmote = ValidationWebhookProcessor.GetCarEmote(webhook.NewRecord.CarId);

                await using var scope = scopeFactory.CreateAsyncScope();

                var messageId = await client.SendMessageAsync($"**New world record!** {envEmote} **{TextFormatter.Deformat(webhook.NewRecord.Map.Name)}**.**{webhook.NewRecord.CarId}** {carEmote} **{new TimeInt32(webhook.NewRecord.Time)}** ({(webhook.NewRecord.Time - webhook.PrevRecord.Time) / 1000f}) by **{TextFormatter.Deformat(webhook.NewRecord.User.Nickname ?? webhook.NewRecord.User.Id)}**");

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var record = await db.Records.FindAsync([webhook.NewRecord.Id], cancellationToken: stoppingToken);

                if (record is null)
                {
                    logger.LogError("Record with ID {RecordId} not found in database for world record webhook", webhook.NewRecord.Id);
                    continue;
                }

                record.WorldRecordMessageDiscordSnowflake = messageId;

                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Processed world record webhook for map {MapName}, new record by {UserNickname} with time {Time}", webhook.NewRecord.Map.Name, webhook.NewRecord.User.Nickname ?? webhook.NewRecord.User.Id, new TimeInt32(webhook.NewRecord.Time));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing world record webhook for map {MapName}, new record by {UserNickname} with time {Time}", webhook.NewRecord.Map.Name, webhook.NewRecord.User.Nickname ?? webhook.NewRecord.User.Id, new TimeInt32(webhook.NewRecord.Time));
            }
        }
    }
}
