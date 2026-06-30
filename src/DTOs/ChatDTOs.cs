namespace IslamiApi.DTOs;

public record SendMessageRequest(int ConversationId, string Message);

public record ConversationDto(int ConversationId, int CustomerId, string Status, string CreatedAt);

public record MessageDto(int Id, int SenderId, string SenderRole, string Message, string CreatedAt);

// WebSocket event payloads
public record ChatWsEvent(string Type, int ConversationId, int SenderId, string SenderRole, string Message, string SentAt);
