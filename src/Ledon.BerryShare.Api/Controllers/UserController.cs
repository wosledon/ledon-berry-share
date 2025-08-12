using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Shared.Entities;
using Ledon.BerryShare.Shared.Querys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledon.BerryShare.Api.Controllers;

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

        var user = await _db.Q<UserEntity>()
            .Include(u => u.Guild)
            .Include(u => u.GiftFlowType)
            .FirstOrDefaultAsync(u => u.Id == userid);
        if (user == null)
        {
            return BerryError("用户不存在");
        }

        var dto = new Ledon.BerryShare.Shared.Results.UserResult
        {
            Id = user.Id,
            Name = user.Name,
            Tel = user.Tel,
            GuildId = user.GuildId ?? Guid.Empty,
            GuildName = user.Guild?.Name ?? string.Empty,
            GiftFlowTypeId = user.GiftFlowTypeId,
            GiftFlowTypeName = user.GiftFlowType?.Name ?? string.Empty,
            CreateAt = user.CreateAt
        };
        return BerryOk(dto);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUserListAsync([FromQuery]UserQuery query)
    {
        var users = await _db.Q<UserEntity>()
            .Include(u => u.Guild)
            .Include(u => u.GiftFlowType)
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(u => u.Name.Contains(query.Search!) || u.Tel.Contains(query.Search!)))
            .WhereIf(query.GuildId.HasValue, q => q.Where(u => u.GuildId == query.GuildId!.Value))
            .OrderByDescending(u => u.CreateAt)
            .Select(u => new Ledon.BerryShare.Shared.Results.UserResult {
                Id = u.Id,
                Name = u.Name,
                Tel = u.Tel,
                GuildId = u.GuildId ?? Guid.Empty,
                GuildName = u.Guild != null ? u.Guild.Name : string.Empty,
                GiftFlowTypeId = u.GiftFlowTypeId,
                GiftFlowTypeName = u.GiftFlowType != null ? u.GiftFlowType.Name : string.Empty,
                CreateAt = u.CreateAt
            })
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserByIdAsync(Guid id)
    {
        var user = await _db.Q<UserEntity>()
            .Include(u => u.Guild)
            .Include(u => u.GiftFlowType)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return BerryError("用户不存在");
        }

        var dto = new Ledon.BerryShare.Shared.Results.UserResult
        {
            Id = user.Id,
            Name = user.Name,
            Tel = user.Tel,
            GuildId = user.GuildId ?? Guid.Empty,
            GuildName = user.Guild?.Name ?? string.Empty,
            GiftFlowTypeId = user.GiftFlowTypeId,
            GiftFlowTypeName = user.GiftFlowType?.Name ?? string.Empty,
            CreateAt = user.CreateAt
        };
        return BerryOk(dto);
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
        existingUser.GuildId = user.GuildId;
        existingUser.GiftFlowTypeId = user.GiftFlowTypeId;

        await _db.SaveChangesAsync();
        
        // 返回更新后的用户信息，包含关联数据
        var updatedUser = await _db.Q<UserEntity>()
            .Include(u => u.Guild)
            .Include(u => u.GiftFlowType)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
            
        if (updatedUser == null)
        {
            return BerryError("用户不存在");
        }
            
        var dto = new Ledon.BerryShare.Shared.Results.UserResult
        {
            Id = updatedUser.Id,
            Name = updatedUser.Name,
            Tel = updatedUser.Tel,
            GuildId = updatedUser.GuildId ?? Guid.Empty,
            GuildName = updatedUser.Guild?.Name ?? string.Empty,
            GiftFlowTypeId = updatedUser.GiftFlowTypeId,
            GiftFlowTypeName = updatedUser.GiftFlowType?.Name ?? string.Empty,
            CreateAt = updatedUser.CreateAt
        };
        
        return BerryOk(dto);
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
        
        // 返回创建后的用户信息，包含关联数据
        var createdUser = await _db.Q<UserEntity>()
            .Include(u => u.Guild)
            .Include(u => u.GiftFlowType)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
            
        if (createdUser == null)
        {
            return BerryError("用户创建失败");
        }
            
        var dto = new Ledon.BerryShare.Shared.Results.UserResult
        {
            Id = createdUser.Id,
            Name = createdUser.Name,
            Tel = createdUser.Tel,
            GuildId = createdUser.GuildId ?? Guid.Empty,
            GuildName = createdUser.Guild?.Name ?? string.Empty,
            GiftFlowTypeId = createdUser.GiftFlowTypeId,
            GiftFlowTypeName = createdUser.GiftFlowType?.Name ?? string.Empty,
            CreateAt = createdUser.CreateAt
        };
        
        return BerryOk(dto);
    }
}
