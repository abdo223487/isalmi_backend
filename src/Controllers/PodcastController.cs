using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;
using IslamiApi.Models;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/podcast")]
[Produces("application/json")]
public class PodcastController(AppDbContext db) : ControllerBase
{
    /// <summary>جلب البودكاست مع pagination</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PodcastResponse>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.Podcasts.CountAsync();
        var items = await db.Podcasts
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PodcastResponse(
                p.Id,
                p.Title,
                p.YoutubeUrl,
                p.Description,
                p.CreatedAt.ToString("O")))
            .ToListAsync();

        return Ok(new PagedResponse<PodcastResponse>(
            total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize),
            items));
    }

    /// <summary>جلب بودكاست بالـ ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PodcastResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Podcasts.FindAsync(id);
        return item == null
            ? NotFound(new ErrorResponse("not_found"))
            : Ok(new PodcastResponse(
                item.Id, item.Title, item.YoutubeUrl,
                item.Description, item.CreatedAt.ToString("O")));
    }

    /// <summary>إضافة بودكاست [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(PodcastResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Add([FromBody] AddPodcastRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new ErrorResponse("title_required"));
        if (string.IsNullOrWhiteSpace(req.YoutubeUrl))
            return BadRequest(new ErrorResponse("youtube_url_required"));

        var item = new Podcast
        {
            Title      = req.Title.Trim(),
            YoutubeUrl = req.YoutubeUrl.Trim(),
            Description = req.Description?.Trim(),
            CreatedAt  = DateTime.UtcNow,
        };

        db.Podcasts.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new PodcastResponse(
                item.Id, item.Title, item.YoutubeUrl,
                item.Description, item.CreatedAt.ToString("O")));
    }

    /// <summary>تعديل بودكاست [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PodcastResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePodcastRequest req)
    {
        var item = await db.Podcasts.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));

        if (!string.IsNullOrWhiteSpace(req.Title))       item.Title      = req.Title.Trim();
        if (!string.IsNullOrWhiteSpace(req.YoutubeUrl))  item.YoutubeUrl = req.YoutubeUrl.Trim();
        if (req.Description is not null)                  item.Description = req.Description.Trim();

        await db.SaveChangesAsync();

        return Ok(new PodcastResponse(
            item.Id, item.Title, item.YoutubeUrl,
            item.Description, item.CreatedAt.ToString("O")));
    }

    /// <summary>حذف بودكاست [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Podcasts.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));

        db.Podcasts.Remove(item);
        await db.SaveChangesAsync();

        return Ok(new MessageResponse("deleted"));
    }
}
