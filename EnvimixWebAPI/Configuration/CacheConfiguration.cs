namespace EnvimixWebAPI.Configuration;

public static class CacheConfiguration
{
    public static void AddCacheServices(this IServiceCollection services)
    {
        services.AddOutputCache();
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024 * 64; // 64 MB
        });
    }
}
