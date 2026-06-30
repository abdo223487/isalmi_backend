namespace IslamiApi.Models;

public class AzkarItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ArabicText { get; set; } = string.Empty;
    public int Repeat { get; set; }
    public int OrderIndex { get; set; }
}
