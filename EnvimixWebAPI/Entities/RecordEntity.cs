using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Entities;

[Index(nameof(Gravity))]
[Index(nameof(Laps))]
public sealed class RecordEntity
{
    public int Id { get; set; }

    public required UserEntity User { get; set; }
    public string UserId { get; set; } = null!;

    public required MapEntity Map { get; set; }
    public string MapId { get; set; } = null!;

    public required CarEntity Car { get; set; }
    public string CarId { get; set; } = null!;

    public required int Gravity { get; set; }

    public required DateTimeOffset DrivenAt { get; set; }

    public EnvimaniaSessionEntity? Session { get; set; } = null!;
    public Guid? SessionId { get; set; }

    public required int Laps { get; set; }

    public GhostEntity? Ghost { get; set; }
    public Guid? GhostId { get; set; }

    public DateTimeOffset? ServersideDrivenAt { get; set; }

    // final time, score, and nb respawns to allow easier grouping
    public required int Time { get; set; }
    public required int Score { get; set; }
    public required int NbRespawns { get; set; }

    public bool Restored { get; set; }

    public ICollection<CheckpointEntity> Checkpoints { get; } = [];

    public bool IsDefaultCar()
    {
        return (Map.Collection == "Canyon" && CarId != "CanyonCar") ||
               (Map.Collection == "Stadium" && CarId != "StadiumCar") ||
               (Map.Collection == "Valley" && CarId != "ValleyCar") ||
               (Map.Collection == "Lagoon" && CarId != "LagoonCar") ||
               (Map.Collection != "Canyon" &&
                Map.Collection != "Stadium" &&
                Map.Collection != "Valley" &&
                Map.Collection != "Lagoon");
    }
}
