
using Discord;
using Discord.Webhook;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
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
            try
            {
                if (!webhook.NewRecord.Map.IsCampaignMap)
                {
                    continue;
                }

                var hasDifferentLapCount = webhook.NewRecord.Map.Laps > 0
                    && webhook.NewRecord.Laps != webhook.NewRecord.Map.Laps;

                if (hasDifferentLapCount && webhook.NewRecord.Laps != 1)
                {
                    logger.LogInformation("Skipping world record webhook for map {MapName} with {Laps} laps because its default is {DefaultLaps} laps", webhook.NewRecord.Map.Name, webhook.NewRecord.Laps, webhook.NewRecord.Map.Laps);
                    continue;
                }

                using var client = new DiscordWebhookClient(config["DiscordRecordWebhook"]);

                var envEmote = ValidationWebhookProcessor.GetEnvEmote(webhook.NewRecord.Map);
                var carEmote = ValidationWebhookProcessor.GetCarEmote(webhook.NewRecord.CarId);
                var mapCarLink = ValidationWebhookProcessor.GetMapCarLink(webhook.NewRecord.Map, webhook.NewRecord.CarId);
                var recordUrl = $"https://envimix.gbx.tools/records/{Uri.EscapeDataString(webhook.NewRecord.Map.Id)}/{Uri.EscapeDataString(webhook.NewRecord.CarId)}/{Uri.EscapeDataString(webhook.NewRecord.User.Id)}/{webhook.NewRecord.Time}";
                var recordTimeLink = $"[`{new TimeInt32(webhook.NewRecord.Time)}`]({recordUrl})";
                var userLink = ValidationWebhookProcessor.GetUserLink(webhook.NewRecord.User);

                var delta = webhook.PrevRecord is null ? null : $" `{(webhook.NewRecord.Time - webhook.PrevRecord.Time) / 1000f:+0.000;-0.000}`";
                var lapCategory = hasDifferentLapCount
                    ? $" **(1 lap)**"
                    : "";

                await using var scope = scopeFactory.CreateAsyncScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                /*var recordCount = await db.Records
                    .Where(x => x.MapId == webhook.NewRecord.Map.Id
                        && x.CarId == webhook.NewRecord.CarId
                        && x.Gravity == webhook.NewRecord.Gravity
                        && x.Laps == webhook.NewRecord.Laps
                        && !x.Removed)
                    .GroupBy(x => x.UserId)
                    .CountAsync(stoppingToken);*/

                var messageId = await client.SendMessageAsync($"**(WR)** {envEmote} {mapCarLink} {carEmote}{lapCategory} {recordTimeLink}{delta} by {userLink} ({TimestampTag.FromDateTimeOffset(webhook.NewRecord.DrivenAt, TimestampTagStyles.ShortTime)})");

                /*if (recordCount > 20)
                {
                    var fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Map & Car").WithValue($"{envEmote} {TextFormatter.Deformat(webhook.NewRecord.Map.Name)}.{webhook.NewRecord.CarId} {carEmote}").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Time").WithValue($"`{new TimeInt32(webhook.NewRecord.Time)}`{delta}").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("By").WithValue($"{TextFormatter.Deformat(webhook.NewRecord.User.Nickname ?? webhook.NewRecord.User.Id)}").WithIsInline(true),
                    };

                    if (webhook.PrevRecord is not null)
                    {
                        fields.Add(new EmbedFieldBuilder().WithName("Previous WR age").WithValue($"{(webhook.NewRecord.DrivenAt - webhook.PrevRecord.DrivenAt).TotalDays} days").WithIsInline(true));

                        if (webhook.PrevRecord.User.Id != webhook.NewRecord.User.Id)
                        {
                            fields.Add(new EmbedFieldBuilder().WithName("Previous WR by").WithValue($"**{TextFormatter.Deformat(webhook.PrevRecord.User.Nickname ?? webhook.PrevRecord.User.Id)}**").WithIsInline(true));
                        }
                    }

                    var embed = new EmbedBuilder()
                        .WithTitle("New world record!")
                        .WithFields(fields)
                        .WithFooter("ENVIMIX Turbo World Records")
                        .WithTimestamp(webhook.NewRecord.DrivenAt)
                        .Build();

                    messageId = await client.SendMessageAsync(embeds: [embed]);
                }
                else
                {
                    messageId = await client.SendMessageAsync($"**(WR)** {envEmote} {TextFormatter.Deformat(webhook.NewRecord.Map.Name)}.{webhook.NewRecord.CarId} {carEmote} `{new TimeInt32(webhook.NewRecord.Time)}`{delta} by {TextFormatter.Deformat(webhook.NewRecord.User.Nickname ?? webhook.NewRecord.User.Id)} ({TimestampTag.FromDateTimeOffset(webhook.NewRecord.DrivenAt, TimestampTagStyles.ShortTime)})");
                }*/

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
