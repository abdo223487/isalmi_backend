namespace IslamiApi.Models;

public class Podcast
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
