using EnvimixWebAPI.Entities;

namespace EnvimixWebAPI.Models;

public sealed record ValidationWebhookDispatch(MapEntity Map, string Car, int Gravity, int Laps);
