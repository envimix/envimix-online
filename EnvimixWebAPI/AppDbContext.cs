using EnvimixWebAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<DiscordUserEntity> DiscordUsers { get; set; }
    public DbSet<ServerEntity> Servers { get; set; }
    public DbSet<EnvimaniaSessionEntity> EnvimaniaSessions { get; set; }
    public DbSet<MapEntity> Maps { get; set; }
    public DbSet<RecordEntity> Records { get; set; }
    public DbSet<CheckpointEntity> Checkpoints { get; set; }
    public DbSet<ZoneEntity> Zones { get; set; }
    public DbSet<CarEntity> Cars { get; set; }
    public DbSet<RatingEntity> Ratings { get; set; }
    public DbSet<StarEntity> Stars { get; set; }
    public DbSet<TitleEntity> Titles { get; set; }
    public DbSet<GhostEntity> Ghosts { get; set; }
    public DbSet<MapVisitEntity> MapVisits { get; set; }
    public DbSet<ValidationDiscordMessageEntity> ValidationDiscordMessages { get; set; }
}