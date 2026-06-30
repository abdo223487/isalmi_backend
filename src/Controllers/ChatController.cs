using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
[Produces("application/json")]
public class ChatController(AppDbContext db) : ControllerBase
{
    /// <summary>المحادثات المنتظرة [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PendingConversationResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetPending()
    {
        var items = await db.ChatConversations
            .Where(c => c.Status == "pending")
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new PendingConversationResponse(
                c.Id, c.CustomerId, c.Status,
                c.CreatedAt.ToString("O"), c.UpdatedAt.ToString("O")))
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>المحادثات النشطة بتاعت الادمن [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<ActiveConversationResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetActive()
    {
        var adminId = GetUserId();
        var items = await db.ChatConversations
            .Where(c => c.Status == "active" && c.AdminId == adminId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ActiveConversationResponse(
                c.Id, c.CustomerId, c.AdminId, c.Status,
                c.CreatedAt.ToString("O"), c.UpdatedAt.ToString("O")))
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>محادثات الـ customer نفسه [Customer]</summary>
    [Authorize(Roles = "customer")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<MyConversationResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetMine()
    {
        var customerId = GetUserId();
        var items = await db.ChatConversations
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new MyConversationResponse(
                c.Id, c.Status,
                c.CreatedAt.ToString("O"), c.UpdatedAt.ToString("O")))
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>جلب رسائل محادثة مع pagination</summary>
    [HttpGet("{conversationId:int}/messages")]
    [ProducesResponseType(typeof(PagedResponse<ChatMessageResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var role = GetRole();

        var conv = await db.ChatConversations.FindAsync(conversationId);
        if (conv == null) return NotFound(new ErrorResponse("conversation_not_found"));

        if (role == "customer" && conv.CustomerId != userId) return Forbid();
        if (role == "admin" && conv.AdminId != null && conv.AdminId != userId) return Forbid();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var total = await db.ChatMessages.CountAsync(m => m.ConversationId == conversationId);
        var messages = await db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatMessageResponse(
                m.Id, m.SenderId, m.SenderRole, m.Message, m.CreatedAt.ToString("O")))
            .ToListAsync();

        return Ok(new PagedResponse<ChatMessageResponse>(
            total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize),
            messages));
    }

    private int GetUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        int.TryParse(sub, out var id);
        return id;
    }

    private string GetRole() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
}
