using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Shared.Entities;
using Ledon.BerryShare.Shared.Querys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledon.BerryShare.Api.Controllers;

[Authorize]
public class UserController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public UserController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var userid = CurrentUser?.Id;
        if (userid == null)
        {
            return BerryError("用户未登录");
        }

        var user = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => u.Id == userid);
        if (user == null)
        {
            return BerryError("用户不存在");
        }

        return BerryOk(user);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUserListAsync(UserQuery query)
    {
        var users = await _db.Q<UserEntity>()
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(u => u.Name.Contains(query.Search!) || u.Tel.Contains(query.Search!)))
            .OrderByDescending(u => u.CreateAt)
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserByIdAsync(Guid id)
    {
        var user = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return BerryError("用户不存在");
        }

        return BerryOk(user);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUserAsync([FromBody] UserEntity user)
    {
        if (user == null || user.Id == Guid.Empty)
        {
            return BerryError("无效的用户信息");
        }

        var existingUser = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existingUser == null)
        {
            return BerryError("用户不存在");
        }

        existingUser.Name = user.Name;
        existingUser.Tel = user.Tel;

        await _db.SaveChangesAsync();
        return BerryOk(existingUser);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BerryError("无效的用户ID");
        }

        var user = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return BerryError("用户不存在");
        }

        _db.Remove(user);
        await _db.SaveChangesAsync();
        return BerryOk();
    }

    [HttpPost("create")]
    public async Task<IActionResult> AddUserAsync([FromBody] UserEntity user)
    {
        if (user == null)
        {
            return BerryError("无效的用户信息");
        }

        if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Tel))
        {
            return BerryError("用户名称和电话不能为空");
        }

        var existingUser = await _db.Q<UserEntity>().FirstOrDefaultAsync(u => u.Tel == user.Tel || u.Name == user.Name);
        if (existingUser != null)
        {
            return BerryError($"用户 {existingUser.Name} : {existingUser.Tel} 已存在");
        }

        _db.Add(user);
        await _db.SaveChangesAsync();
        return BerryOk(user);
    }
}
