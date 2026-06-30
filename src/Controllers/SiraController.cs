using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;
using IslamiApi.Models;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/sira")]
[Produces("application/json")]
public class SiraController(AppDbContext db) : ControllerBase
{
    /// <summary>جلب السيرة مع pagination</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SiraResponse>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.Siras.CountAsync();
        var items = await db.Siras
            .OrderBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SiraResponse(s.Id, s.Content))
            .ToListAsync();

        return Ok(new PagedResponse<SiraResponse>(
            total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize),
            items));
    }

    /// <summary>جلب مقطع سيرة بالـ ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SiraResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Siras.FindAsync(id);
        return item == null
            ? NotFound(new ErrorResponse("not_found"))
            : Ok(new SiraResponse(item.Id, item.Content));
    }

    /// <summary>إضافة مقاطع سيرة كتير دفعة واحدة [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkAddedResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> AddBulk([FromBody] AddSirasRequest req)
    {
        if (req.Contents == null || req.Contents.Count == 0)
            return BadRequest(new ErrorResponse("empty_list"));

        var items = req.Contents
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => new Sira { Content = c.Trim() })
            .ToList();

        db.Siras.AddRange(items);
        await db.SaveChangesAsync();
        return Ok(new BulkAddedResponse(items.Count));
    }

    /// <summary>إضافة مقطع سيرة واحد [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(SiraResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> AddSingle([FromBody] UpdateSiraRequest req)
    {
        var item = new Sira { Content = req.Content };
        db.Siras.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new SiraResponse(item.Id, item.Content));
    }

    /// <summary>تعديل مقطع سيرة [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SiraResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSiraRequest req)
    {
        var item = await db.Siras.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));
        item.Content = req.Content;
        await db.SaveChangesAsync();
        return Ok(new SiraResponse(item.Id, item.Content));
    }

    /// <summary>حذف مقطع سيرة [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Siras.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));
        db.Siras.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse("deleted"));
    }
}
