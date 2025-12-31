using dotnetweb.Data;
using dotnetweb.DTOs;
using dotnetweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnetweb.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;

    public UserController(UserManager<User> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst("id")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

        var profile = new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WalletBalance = wallet?.Balance ?? 0,
            Currency = wallet?.Currency ?? "USD"
        };

        return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile, "Profile retrieved successfully"));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
    {
        var userId = User.FindFirst("id")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        user.FullName = model.FullName;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { user.FullName }, "Profile updated successfully"));
        }

        return BadRequest(ApiResponse<object>.ErrorResponse("Failed to update profile", result.Errors.Select(e => e.Description).ToList()));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        var userId = User.FindFirst("id")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Password changed successfully"));
        }

        return BadRequest(ApiResponse<object>.ErrorResponse("Failed to change password", result.Errors.Select(e => e.Description).ToList()));
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirst("id")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        // Delete associated wallet and transactions
        var wallet = await _context.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet != null)
        {
            var transactions = await _context.Transactions.Where(t => t.WalletId == wallet.Id).ToListAsync();
            _context.Transactions.RemoveRange(transactions);
            _context.Wallets.Remove(wallet);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Database error while deleting account data", new List<string> { inner }));
            }
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Account deleted successfully"));
        }

        return BadRequest(ApiResponse<object>.ErrorResponse("Failed to delete account", result.Errors.Select(e => e.Description).ToList()));
    }
}
