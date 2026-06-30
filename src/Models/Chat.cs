namespace IslamiApi.Models;

public class ChatConversation
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? AdminId { get; set; }
    public string Status { get; set; } = "pending"; // pending | active | closed
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderRole { get; set; } = string.Empty; // admin | customer
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
