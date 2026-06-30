using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;
using IslamiApi.Models;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/hadith")]
[Produces("application/json")]
public class HadithController(AppDbContext db) : ControllerBase
{
    /// <summary>جلب الأحاديث مع pagination</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<HadithResponse>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.Hadiths.CountAsync();
        var items = await db.Hadiths
            .OrderBy(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HadithResponse(h.Id, h.Content))
            .ToListAsync();

        return Ok(new PagedResponse<HadithResponse>(
            total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize),
            items));
    }

    /// <summary>جلب حديث بالـ ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HadithResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Hadiths.FindAsync(id);
        return item == null
            ? NotFound(new ErrorResponse("not_found"))
            : Ok(new HadithResponse(item.Id, item.Content));
    }

    /// <summary>جلب حديث عشوائي</summary>
    [HttpGet("random")]
    [ProducesResponseType(typeof(HadithResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetRandom()
    {
        var count = await db.Hadiths.CountAsync();
        if (count == 0) return NotFound(new ErrorResponse("no_hadiths"));
        var skip = Random.Shared.Next(count);
        var item = await db.Hadiths.Skip(skip).FirstAsync();
        return Ok(new HadithResponse(item.Id, item.Content));
    }

    /// <summary>إضافة أحاديث كتير دفعة واحدة [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkAddedResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> AddBulk([FromBody] AddHadithsRequest req)
    {
        if (req.Contents == null || req.Contents.Count == 0)
            return BadRequest(new ErrorResponse("empty_list"));

        var items = req.Contents
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => new Hadith { Content = c.Trim() })
            .ToList();

        db.Hadiths.AddRange(items);
        await db.SaveChangesAsync();
        return Ok(new BulkAddedResponse(items.Count));
    }

    /// <summary>إضافة حديث واحد [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(HadithResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> AddSingle([FromBody] UpdateHadithRequest req)
    {
        var item = new Hadith { Content = req.Content };
        db.Hadiths.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new HadithResponse(item.Id, item.Content));
    }

    /// <summary>تعديل حديث [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HadithResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHadithRequest req)
    {
        var item = await db.Hadiths.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));
        item.Content = req.Content;
        await db.SaveChangesAsync();
        return Ok(new HadithResponse(item.Id, item.Content));
    }

    /// <summary>حذف حديث [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Hadiths.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));
        db.Hadiths.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse("deleted"));
    }
}
