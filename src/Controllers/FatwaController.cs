using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;
using IslamiApi.Models;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/fatwa")]
[Produces("application/json")]
public class FatwaController(AppDbContext db) : ControllerBase
{
    /// <summary>جلب الفتاوى مع pagination</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<FatwaResponse>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.Fatwas.CountAsync();
        var items = await db.Fatwas
            .OrderBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FatwaResponse(f.Id, f.Title, f.Content))
            .ToListAsync();

        return Ok(new PagedResponse<FatwaResponse>(
            total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize),
            items));
    }

    /// <summary>جلب فتوى بالـ ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FatwaResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Fatwas.FindAsync(id);
        return item == null
            ? NotFound(new ErrorResponse("not_found"))
            : Ok(new FatwaResponse(item.Id, item.Title, item.Content));
    }

    /// <summary>إضافة فتوى [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(FatwaResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Add([FromBody] AddFatwaRequest req)
    {
        var item = new Fatwa { Title = req.Title, Content = req.Content };
        db.Fatwas.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new FatwaResponse(item.Id, item.Title, item.Content));
    }

    /// <summary>تعديل فتوى [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FatwaResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFatwaRequest req)
    {
        var item = await db.Fatwas.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));

        if (req.Title != null) item.Title = req.Title;
        if (req.Content != null) item.Content = req.Content;

        await db.SaveChangesAsync();
        return Ok(new FatwaResponse(item.Id, item.Title, item.Content));
    }

    /// <summary>حذف فتوى [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Fatwas.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));
        db.Fatwas.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse("deleted"));
    }
}
