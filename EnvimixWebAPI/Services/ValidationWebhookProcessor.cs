
using Discord;
using Discord.Webhook;
using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using System.Threading.Channels;
using TmEssentials;

namespace EnvimixWebAPI.Services;

public sealed class ValidationWebhookProcessor : BackgroundService
{
    private readonly Channel<ValidationWebhookDispatch> webhookChannel;
    private readonly IConfiguration config;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ValidationWebhookProcessor> logger;

    public ValidationWebhookProcessor(Channel<ValidationWebhookDispatch> webhookChannel, IConfiguration config, IServiceScopeFactory scopeFactory, ILogger<ValidationWebhookProcessor> logger)
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
                // hack but works for majority of maps
                if (webhook.Map.Collection == "Canyon" && webhook.Car == "CanyonCar"
                    || webhook.Map.Collection == "Valley" && webhook.Car == "ValleyCar"
                    || webhook.Map.Collection == "Lagoon" && webhook.Car == "LagoonCar"
                    || webhook.Map.Collection == "Stadium" && webhook.Car == "StadiumCar")
                {
                    continue;
                }

                if (webhook.Gravity != 0)
                {
                    logger.LogWarning("Skipping validation webhook for map {MapId} with gravity {Gravity}", webhook.Map.Id, webhook.Gravity);
                    continue;
                }

                using var client = new DiscordWebhookClient(config["DiscordValidationWebhook"]);

                var envEmote = webhook.Map.Collection switch
                {
                    "Canyon" => "<:CanyonTMT:1441220239894253710>",
                    "Valley" => "<:ValleyTMT:1441220278922514482>",
                    "Lagoon" => "<:LagoonTMT:1441220242184339510>",
                    "Stadium" => "<:StadiumTMT:1441220247100067900>",
                    _ => "",
                };

                var carEmote = webhook.Car switch
                {
                    "CanyonCar" => "<:CanyonCar:1422392873927839794>",
                    "ValleyCar" => "<:ValleyCar:1422393207697969152>",
                    "LagoonCar" => "<:LagoonCar:1422392498835165296>",
                    "StadiumCar" => "<:StadiumCarTM2:1441092118683586690>",
                    "TrafficCar" => "<:TrafficCar:1441122726831063163>",
                    "IslandCar" => "<:IslandCar:1441120664529535046>",
                    "BayCar" => "<:BayCar:1420806883794616370>",
                    "CoastCar" => "<:CoastCar:1420806241986281592>",
                    "DesertCar" => "<:DesertCar:1441120705759547392>",
                    "RallyCar" => "<:RallyCar:1420806885300502699>",
                    "SnowCar" => "<:SnowCar:1420806887913426954>",
                    _ => "",
                };

                await using var scope = scopeFactory.CreateAsyncScope();

                var envimaniaService = scope.ServiceProvider.GetRequiredService<IEnvimaniaService>();

                var validation = await envimaniaService.GetValidationAsync(webhook.Map.Id, new EnvimaniaRecordFilter { Car = webhook.Car, Gravity = webhook.Gravity, Laps = webhook.Laps }, CancellationToken.None);

                var messageId = validation is null
                    ? await client.SendMessageAsync($"{envEmote} **{TextFormatter.Deformat(webhook.Map.Name)}**.**{webhook.Car}** {carEmote} probably validated")
                    : await client.SendMessageAsync($"{envEmote} **{TextFormatter.Deformat(webhook.Map.Name)}**.**{webhook.Car}** {carEmote} validated by **{TextFormatter.Deformat(validation.User.Nickname ?? validation.User.Id)}**");

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.ValidationDiscordMessages.AddAsync(new ValidationDiscordMessageEntity
                {
                    Id = messageId,
                    Record = validation,
                }, stoppingToken);

                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Sent validation webhook for map {MapId}, message ID {MessageId}", webhook.Map.Id, messageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing validation webhook for map {MapId}", webhook.Map.Id);
            }
        }
    }
}
