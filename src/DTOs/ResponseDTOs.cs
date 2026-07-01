namespace IslamiApi.DTOs;

// ── Generic
public record MessageResponse(string Message);
public record ErrorResponse(string Error);

// ── Auth
public record RegisterResponse(string Message);
public record LoginResponse(string AccessToken, string RefreshToken, string Role);
public record MeResponse(int Id, string Email, string Name, string Role, string CreatedAt);

// ── Pagination wrapper
public record PagedResponse<T>(int Total, int Page, int PageSize, int TotalPages, List<T> Data);

// ── Azkar
public record AzkarResponse(int Id, string Category, string ArabicText, int Repeat, int OrderIndex);

// ── Fatwa
public record FatwaResponse(int Id, string Title, string Content);

// ── Hadith
public record HadithResponse(int Id, string Content);
public record BulkAddedResponse(int Added);

// ── Sira
public record SiraResponse(int Id, string Content);

// ── Chat REST
public record PendingConversationResponse(int Id, int CustomerId, string Status, string CreatedAt, string UpdatedAt);
public record ActiveConversationResponse(int Id, int CustomerId, int? AdminId, string Status, string CreatedAt, string UpdatedAt);
public record MyConversationResponse(int Id, string Status, string CreatedAt, string UpdatedAt);
public record ChatMessageResponse(int Id, int SenderId, string SenderRole, string Message, string CreatedAt);

// ── Podcast
public record PodcastResponse(int Id, string Title, string YoutubeUrl, string? Description, string CreatedAt);
