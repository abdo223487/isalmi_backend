using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;
using IslamiApi.Models;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/azkar")]
[Produces("application/json")]
public class AzkarController(AppDbContext db) : ControllerBase
{
    /// <summary>جلب كل الأذكار مرتبة بالـ category ثم orderIndex</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AzkarResponse>), 200)]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.AzkarItems
            .OrderBy(a => a.Category).ThenBy(a => a.OrderIndex)
            .Select(a => new AzkarResponse(a.Id, a.Category, a.ArabicText, a.Repeat, a.OrderIndex))
            .ToListAsync());

    /// <summary>جلب كل الـ categories المتاحة</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetCategories() =>
        Ok(await db.AzkarItems.Select(a => a.Category).Distinct().OrderBy(c => c).ToListAsync());

    /// <summary>جلب أذكار category معينة</summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(List<AzkarResponse>), 200)]
    public async Task<IActionResult> GetByCategory(string category) =>
        Ok(await db.AzkarItems
            .Where(a => a.Category.ToLower() == category.ToLower())
            .OrderBy(a => a.OrderIndex)
            .Select(a => new AzkarResponse(a.Id, a.Category, a.ArabicText, a.Repeat, a.OrderIndex))
            .ToListAsync());

    /// <summary>جلب ذكر بالـ ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AzkarResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.AzkarItems.FindAsync(id);
        return item == null
            ? NotFound(new ErrorResponse("not_found"))
            : Ok(new AzkarResponse(item.Id, item.Category, item.ArabicText, item.Repeat, item.OrderIndex));
    }

    /// <summary>إضافة ذكر جديد [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(AzkarResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Add([FromBody] AddAzkarRequest req)
    {
        var item = new AzkarItem
        {
            Category = req.Category,
            ArabicText = req.ArabicText,
            Repeat = req.Repeat,
            OrderIndex = req.OrderIndex,
        };
        db.AzkarItems.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id },
            new AzkarResponse(item.Id, item.Category, item.ArabicText, item.Repeat, item.OrderIndex));
    }

    /// <summary>تعديل ذكر [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AzkarResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAzkarRequest req)
    {
        var item = await db.AzkarItems.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));

        if (req.ArabicText != null) item.ArabicText = req.ArabicText;
        if (req.Repeat.HasValue) item.Repeat = req.Repeat.Value;

        await db.SaveChangesAsync();
        return Ok(new AzkarResponse(item.Id, item.Category, item.ArabicText, item.Repeat, item.OrderIndex));
    }

    /// <summary>حذف ذكر [Admin]</summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.AzkarItems.FindAsync(id);
        if (item == null) return NotFound(new ErrorResponse("not_found"));
        db.AzkarItems.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse("deleted"));
    }
}
