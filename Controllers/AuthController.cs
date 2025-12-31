using dotnetweb.Data;
using dotnetweb.DTOs;
using dotnetweb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace dotnetweb.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext context, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", errors));
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("User with this email already exists"));
        }

        var user = new User { UserName = model.Email, Email = model.Email, FullName = model.FullName };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Create Wallet
            var wallet = new Wallet { UserId = user.Id, Balance = 0 };
            _context.Wallets.Add(wallet);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Database error while creating wallet", new List<string> { inner }));
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { UserId = user.Id, Email = user.Email }, "User registered successfully"));
        }

        var identityErrors = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(ApiResponse<object>.ErrorResponse("Registration failed", identityErrors));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid email or password"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (result.Succeeded)
        {
            var token = GenerateJwtToken(user);
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                User = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName
                }
            }, "Login successful"));
        }

        return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid email or password"));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var userId = User.FindFirst("id")?.Value;
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not found"));
        }

        var token = GenerateJwtToken(user);
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddDays(7)
        }, "Token refreshed successfully"));
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim("id", user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
