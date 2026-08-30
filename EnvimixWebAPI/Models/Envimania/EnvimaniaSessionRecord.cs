namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaSessionRecord(
    string UserLogin,
    string? Nickname,
    string Car,
    int Laps,
    int Time,
    int Score,
    int NbRespawns,
    DateTimeOffset DrivenAt);
