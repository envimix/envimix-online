using EnvimixDiscordBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixDiscordBot;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<CampaignModel> Campaigns { get; set; }
    public DbSet<ConvertedMapModel> ConvertedMaps { get; set; }
    public DbSet<CarModel> Cars { get; set; }
}
