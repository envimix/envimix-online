namespace EnvimixWebAPI.Options;

public class EnvimaniaOptions
{
    public HashSet<string> Car { get; set; } = [];
    public Dictionary<string, string> PlayerModel { get; set; } = [];
    public HashSet<int> Gravity { get; set; } = [];
    public HashSet<string> Accel { get; set; } = [];
}
