namespace IslamiApi.DTOs;

// ── Azkar
public record AddAzkarRequest(string Category, string ArabicText, int Repeat, int OrderIndex);
public record UpdateAzkarRequest(string? ArabicText, int? Repeat);

// ── Fatwa
public record AddFatwaRequest(string Title, string Content);
public record UpdateFatwaRequest(string? Title, string? Content);

// ── Hadith
public record AddHadithsRequest(List<string> Contents);
public record UpdateHadithRequest(string Content);

// ── Sira
public record AddSirasRequest(List<string> Contents);
public record UpdateSiraRequest(string Content);

// ── Podcast
public record AddPodcastRequest(string Title, string YoutubeUrl, string? Description);
public record UpdatePodcastRequest(string? Title, string? YoutubeUrl, string? Description);
