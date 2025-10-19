namespace EnvimixWebAPI.Options;

public class JwtOptions
{
    public required string Key { get; set; }
    public required string Issuer { get; set; }
}
