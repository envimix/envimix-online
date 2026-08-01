namespace EnvimixWebAPI.Models;

public sealed class CompletionMedals
{
    public int Ducks { get; set; }
    public int STMs { get; set; }
    public int SuperGolds { get; set; }
    public int SuperSilvers { get; set; }
    public int SuperBronzes { get; set; }
    public int AuthorMedals { get; set; }
    public int GoldMedals { get; set; }
    public int SilverMedals { get; set; }
    public int BronzeMedals { get; set; }
    public int Total => Ducks + STMs + SuperGolds + SuperSilvers + SuperBronzes + AuthorMedals + GoldMedals + SilverMedals + BronzeMedals;
}