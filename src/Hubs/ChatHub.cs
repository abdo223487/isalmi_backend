using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.Models;

namespace IslamiApi.Hubs;

[Authorize]
public class ChatHub(AppDbContext db) : Hub
{
    // ── Called by customer: start a new chat request
    public async Task RequestChat()
    {
        var (userId, role) = GetUser();
        if (role != "customer")
        {
            await Clients.Caller.SendAsync("error", new { error = "only_customers_can_request" });
            return;
        }

        // Check if already has active/pending conversation
        var existing = await db.ChatConversations
            .FirstOrDefaultAsync(c => c.CustomerId == userId && c.Status != "closed");

        if (existing != null)
        {
            await Clients.Caller.SendAsync("chatAlreadyExists", new
            {
                conversationId = existing.Id,
                status = existing.Status,
            });
            return;
        }

        var conv = new ChatConversation
        {
            CustomerId = userId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.ChatConversations.Add(conv);
        await db.SaveChangesAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, conv.Id.ToString());

        // Notify all admins
        await Clients.Group("admins").SendAsync("newChatRequest", new
        {
            conversationId = conv.Id,
            customerId = userId,
            createdAt = conv.CreatedAt.ToString("O"),
        });

        await Clients.Caller.SendAsync("chatRequested", new
        {
            conversationId = conv.Id,
            status = "pending",
        });
    }

    // ── Called by admin: accept a pending chat
    public async Task AcceptChat(int conversationId)
    {
        var (userId, role) = GetUser();
        if (role != "admin")
        {
            await Clients.Caller.SendAsync("error", new { error = "only_admins_can_accept" });
            return;
        }

        var conv = await db.ChatConversations.FindAsync(conversationId);
        if (conv == null)
        {
            await Clients.Caller.SendAsync("error", new { error = "conversation_not_found" });
            return;
        }
        if (conv.Status != "pending")
        {
            await Clients.Caller.SendAsync("error", new { error = "conversation_not_pending" });
            return;
        }

        conv.AdminId = userId;
        conv.Status = "active";
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

        await Clients.Group(conversationId.ToString()).SendAsync("chatAccepted", new
        {
            conversationId,
            adminId = userId,
            status = "active",
        });
    }

    // ── Called by customer or admin: send a message
    public async Task SendMessage(int conversationId, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var (userId, role) = GetUser();

        var conv = await db.ChatConversations.FindAsync(conversationId);
        if (conv == null)
        {
            await Clients.Caller.SendAsync("error", new { error = "conversation_not_found" });
            return;
        }
        if (conv.Status == "closed")
        {
            await Clients.Caller.SendAsync("error", new { error = "conversation_closed" });
            return;
        }

        // Authorization check
        if (role == "customer" && conv.CustomerId != userId)
        {
            await Clients.Caller.SendAsync("error", new { error = "unauthorized" });
            return;
        }
        if (role == "admin" && conv.AdminId != userId)
        {
            await Clients.Caller.SendAsync("error", new { error = "not_your_conversation" });
            return;
        }

        var msg = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = userId,
            SenderRole = role,
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        db.ChatMessages.Add(msg);
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await Clients.Group(conversationId.ToString()).SendAsync("newMessage", new
        {
            id = msg.Id,
            conversationId,
            senderId = userId,
            senderRole = role,
            message = msg.Message,
            createdAt = msg.CreatedAt.ToString("O"),
        });
    }

    // ── Called by customer/admin: join conversation room
    public async Task JoinConversation(int conversationId)
    {
        var (userId, role) = GetUser();

        var conv = await db.ChatConversations.FindAsync(conversationId);
        if (conv == null)
        {
            await Clients.Caller.SendAsync("error", new { error = "conversation_not_found" });
            return;
        }

        // Validate access
        if (role == "customer" && conv.CustomerId != userId)
        {
            await Clients.Caller.SendAsync("error", new { error = "unauthorized" });
            return;
        }
        if (role == "admin" && conv.AdminId != null && conv.AdminId != userId)
        {
            await Clients.Caller.SendAsync("error", new { error = "not_your_conversation" });
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        await Clients.Caller.SendAsync("joinedConversation", new { conversationId });
    }

    // ── Called by admin: close a conversation
    public async Task CloseChat(int conversationId)
    {
        var (userId, role) = GetUser();
        if (role != "admin")
        {
            await Clients.Caller.SendAsync("error", new { error = "only_admins_can_close" });
            return;
        }

        var conv = await db.ChatConversations.FindAsync(conversationId);
        if (conv == null || conv.AdminId != userId)
        {
            await Clients.Caller.SendAsync("error", new { error = "not_found_or_unauthorized" });
            return;
        }

        conv.Status = "closed";
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await Clients.Group(conversationId.ToString()).SendAsync("chatClosed", new
        {
            conversationId,
            closedAt = conv.UpdatedAt.ToString("O"),
        });
    }

    // ── Admin joins admin group on connect
    public async Task JoinAdminGroup()
    {
        var (_, role) = GetUser();
        if (role == "admin")
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    // ── helpers
    private (int userId, string role) GetUser()
    {
        var sub = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? Context.User?.FindFirst("sub")?.Value;
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "customer";
        int.TryParse(sub, out var userId);
        return (userId, role);
    }
}
