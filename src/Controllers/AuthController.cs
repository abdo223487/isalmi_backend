using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IslamiApi.Data;
using IslamiApi.DTOs;
using IslamiApi.Models;
using IslamiApi.Services;

namespace IslamiApi.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private string JwtSecret => config["Jwt:Secret"]
        ?? "change_this_super_secret_key_in_production_min_32_chars";

    /// <summary>تسجيل مستخدم أو ادمن جديد</summary>
    /// <remarks>لتسجيل ادمن: أضف @admin في الباسورد مثلاً "pass1234@admin"</remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrEmpty(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new ErrorResponse("invalid_email"));

        var isAdmin = req.Password.Contains("@admin");
        var cleanPassword = req.Password.Replace("@admin", "");

        if (cleanPassword.Length < 8)
            return BadRequest(new ErrorResponse("password_too_short"));

        var email = req.Email.ToLower();

        if (isAdmin)
        {
            if (await db.Admins.AnyAsync(a => a.Email == email))
                return Conflict(new ErrorResponse("email_already_exists"));

            db.Admins.Add(new Admin
            {
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(cleanPassword),
                Name = req.Name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            });
        }
        else
        {
            if (await db.Customers.AnyAsync(c => c.Email == email))
                return Conflict(new ErrorResponse("email_already_exists"));

            db.Customers.Add(new Customer
            {
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(cleanPassword),
                Name = req.Name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync();
        return Ok(new MessageResponse("registered"));
    }

    /// <summary>تسجيل الدخول</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var cleanPassword = req.Password.Replace("@admin", "");
        var email = req.Email.ToLower();

        var admin = await db.Admins.FirstOrDefaultAsync(a => a.Email == email);
        if (admin != null)
        {
            if (!admin.IsActive) return Unauthorized(new ErrorResponse("account_disabled"));
            if (!PasswordHelper.VerifyPassword(cleanPassword, admin.PasswordHash))
                return Unauthorized(new ErrorResponse("invalid_credentials"));

            await RevokeTokens(admin.Id, isAdmin: true);
            var tokens = await GenerateAndSaveTokens(admin.Id, admin.Email, "admin", isAdmin: true);
            return Ok(tokens);
        }

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (customer == null) return Unauthorized(new ErrorResponse("invalid_credentials"));
        if (!customer.IsActive) return Unauthorized(new ErrorResponse("account_disabled"));
        if (!PasswordHelper.VerifyPassword(cleanPassword, customer.PasswordHash))
            return Unauthorized(new ErrorResponse("invalid_credentials"));

        await RevokeTokens(customer.Id, isAdmin: false);
        var customerTokens = await GenerateAndSaveTokens(customer.Id, customer.Email, "customer", isAdmin: false);
        return Ok(customerTokens);
    }

    /// <summary>تجديد الـ access token باستخدام refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken);
        if (stored == null) return Unauthorized(new ErrorResponse("invalid_refresh_token"));

        if (stored.IsRevoked)
        {
            await RevokeTokens(stored.UserId, stored.IsAdmin);
            return Unauthorized(new ErrorResponse("refresh_token_reused"));
        }

        if (DateTime.UtcNow > stored.ExpiresAt)
        {
            db.RefreshTokens.Remove(stored);
            await db.SaveChangesAsync();
            return Unauthorized(new ErrorResponse("refresh_token_expired"));
        }

        stored.IsRevoked = true;
        await db.SaveChangesAsync();

        if (stored.IsAdmin)
        {
            var admin = await db.Admins.FindAsync(stored.UserId);
            if (admin == null || !admin.IsActive) return Unauthorized(new ErrorResponse("user_not_found"));
            return Ok(await GenerateAndSaveTokens(admin.Id, admin.Email, "admin", isAdmin: true));
        }
        else
        {
            var customer = await db.Customers.FindAsync(stored.UserId);
            if (customer == null || !customer.IsActive) return Unauthorized(new ErrorResponse("user_not_found"));
            return Ok(await GenerateAndSaveTokens(customer.Id, customer.Email, "customer", isAdmin: false));
        }
    }

    /// <summary>تسجيل الخروج من الجهاز الحالي</summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken);
        if (stored != null)
        {
            stored.IsRevoked = true;
            await db.SaveChangesAsync();
        }
        return Ok(new MessageResponse("logged_out"));
    }

    /// <summary>تسجيل الخروج من كل الأجهزة</summary>
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    public async Task<IActionResult> LogoutAll([FromBody] LogoutRequest req)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken);
        if (stored != null)
            await RevokeTokens(stored.UserId, stored.IsAdmin);
        return Ok(new MessageResponse("all_sessions_revoked"));
    }

    /// <summary>بيانات المستخدم الحالي</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var role = GetRole();
        if (userId == null) return Unauthorized(new ErrorResponse("unauthorized"));

        if (role == "admin")
        {
            var admin = await db.Admins.FindAsync(userId.Value);
            if (admin == null) return NotFound(new ErrorResponse("user_not_found"));
            return Ok(new MeResponse(admin.Id, admin.Email, admin.Name ?? "", "admin", admin.CreatedAt.ToString("O")));
        }
        else
        {
            var customer = await db.Customers.FindAsync(userId.Value);
            if (customer == null) return NotFound(new ErrorResponse("user_not_found"));
            return Ok(new MeResponse(customer.Id, customer.Email, customer.Name ?? "", "customer", customer.CreatedAt.ToString("O")));
        }
    }

    /// <summary>تغيير كلمة المرور</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = GetUserId();
        var role = GetRole();
        if (userId == null) return Unauthorized(new ErrorResponse("unauthorized"));
        if (req.NewPassword.Length < 8) return BadRequest(new ErrorResponse("password_too_short"));

        if (role == "admin")
        {
            var admin = await db.Admins.FindAsync(userId.Value);
            if (admin == null) return NotFound(new ErrorResponse("user_not_found"));
            if (!PasswordHelper.VerifyPassword(req.OldPassword, admin.PasswordHash))
                return BadRequest(new ErrorResponse("invalid_old_password"));

            admin.PasswordHash = PasswordHelper.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();
            await RevokeTokens(userId.Value, isAdmin: true);
        }
        else
        {
            var customer = await db.Customers.FindAsync(userId.Value);
            if (customer == null) return NotFound(new ErrorResponse("user_not_found"));
            if (!PasswordHelper.VerifyPassword(req.OldPassword, customer.PasswordHash))
                return BadRequest(new ErrorResponse("invalid_old_password"));

            customer.PasswordHash = PasswordHelper.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();
            await RevokeTokens(userId.Value, isAdmin: false);
        }

        return Ok(new MessageResponse("password_changed"));
    }

    private async Task<LoginResponse> GenerateAndSaveTokens(int id, string email, string role, bool isAdmin)
    {
        var accessToken = JwtHelper.GenerateAccessToken(id, email, role, JwtSecret);
        var refreshToken = JwtHelper.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = id,
            Token = refreshToken,
            IsAdmin = isAdmin,
            ExpiresAt = JwtHelper.RefreshTokenExpiry,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
        });
        await db.SaveChangesAsync();

        return new LoginResponse(accessToken, refreshToken, role);
    }

    private async Task RevokeTokens(int userId, bool isAdmin)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.IsAdmin == isAdmin && !t.IsRevoked)
            .ToListAsync();
        foreach (var t in tokens) t.IsRevoked = true;
        await db.SaveChangesAsync();
    }

    private int? GetUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    private string? GetRole() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
}
