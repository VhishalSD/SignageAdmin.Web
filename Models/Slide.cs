namespace SignageApp.Models;

public class Slide
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Bedrijf { get; set; } = "";
    public string Status { get; set; } = "";
    public string Titel { get; set; } = "";
    public string Plaats { get; set; } = "";
    public string Prijs { get; set; } = "";
    public string EnergieLabel { get; set; } = "";
    public string Afbeelding { get; set; } = "";
    public string Extra { get; set; } = "";
    public bool IsActief { get; set; }
    public int Volgorde { get; set; }
    public int DurationSeconds { get; set; } = 10;
}