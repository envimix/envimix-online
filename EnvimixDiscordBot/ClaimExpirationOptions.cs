using Microsoft.Extensions.Configuration;

namespace EnvimixDiscordBot;

public sealed class ClaimExpirationOptions
{
    public int Hours { get; }
    public TimeSpan Duration { get; }

    public ClaimExpirationOptions(IConfiguration configuration)
    {
        Hours = configuration.GetValue<int?>("ClaimExpiration:Hours")
            ?? throw new InvalidOperationException("ClaimExpiration:Hours is required.");

        if (Hours <= 0)
        {
            throw new InvalidOperationException("ClaimExpiration:Hours must be greater than zero.");
        }

        Duration = TimeSpan.FromHours(Hours);
    }
}
