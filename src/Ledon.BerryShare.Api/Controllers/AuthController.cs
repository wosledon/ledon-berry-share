using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Results;

namespace Ledon.BerryShare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IConfiguration _config;
    private readonly UnitOfWork _db;

    public AuthController(IConfiguration config,
        UnitOfWork db)
    {
        _config = config;
        _db = db;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BerryError("用户名或密码不能为空");
        }

        var user = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => (u.Account == request.UserName
        || u.Tel == request.UserName) && u.Password == request.Password);

        if (user is null)
        {
            return BerryUnauthorized("用户名或密码错误");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Account),
            new Claim(ClaimTypes.Role, user.Role.ToString() ?? "User"),
            new Claim("UserId", user.Id.ToString()),
            new Claim("Tel", user.Tel ?? string.Empty),
        };

        var jwtKey = _config["Jwt:Key"] ?? "default_jwt_key";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );
        return BerryOk(new LoginResult{ Token = new JwtSecurityTokenHandler().WriteToken(token) });

    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return BerryOk("退出成功");
    }

    [HttpPost("reset-pwd")]
    [Authorize]
    public async Task<IActionResult> ResetPassword([FromBody] string newPassword)
    {
        var userId = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BerryUnauthorized("未授权");
        }

        var user = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => u.Id.ToString() == userId);
        if (user is null)
        {
            return BerryNotFound("用户未找到");
        }

        user.Password = newPassword;
        await _db.SaveChangesAsync();

        return BerryOk("密码重置成功");
    }
}

public class LoginRequest
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
}