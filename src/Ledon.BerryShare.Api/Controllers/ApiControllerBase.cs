using System;
using Ledon.BerryShare.Shared;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Ledon.BerryShare.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApiControllerBase : ControllerBase
{

    public UserEntity? CurrentUser
    {
        get
        {
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst("Role")?.Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return null;

            var tel = User.FindFirst("Tel")?.Value ?? string.Empty;
            var name = User.Identity?.Name ?? string.Empty;

            return new UserEntity
            {
                Id = Guid.Parse(userId),
                Role = Enum.Parse<UserEntity.RoleEnum>(role),
                Tel = tel,
                Name = name
            };
        }
    }

    [NonAction]
    public IActionResult BerryOk<T>(T data) where T : new()
    {
        return Ok(new BerryResult<T>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = data
        });
    }

    [NonAction]
    public IActionResult BerryOk(string message = "请求成功")
    {
        return Ok(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Message = message
        });
    }

    [NonAction]
    public IActionResult BerryError(string message = "请求失败")
    {
        return Ok(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.Error,
            Message = message,
        });
    }

    [NonAction]
    public IActionResult BerryNotFound(string message = "资源未找到", object? data = null)
    {
        return Ok(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.NotFound,
            Message = message,
        });
    }

    [NonAction]
    public IActionResult BerryUnauthorized(string message = "未授权", object? data = null)
    { 
        return Ok(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.Unauthorized,
            Message = message,
        });
    }
}