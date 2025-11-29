using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnvimixWebAPI.Configuration;

public static class DataConfiguration
{
    public static void AddDataServices(this IServiceCollection services, IConfiguration config, IHostEnvironment hostEnvironment)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionStr = config.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr));

            if (!hostEnvironment.IsDevelopment())
            {
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuted)); // should be configurable
            }
        });
    }

    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
}
