
using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared.Entities;
using Ledon.BerryShare.Shared.Querys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledon.BerryShare.Api.Controllers;

[Authorize]
public class GuildController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public GuildController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetGuildListAsync(GuildQuery query)
    {
        var guilds = await _db.Q<GuildEntity>()
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(g => g.Name.Contains(query.Search!)))
            .OrderByDescending(g => g.CreateAt)
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(guilds);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGuildByIdAsync(Guid id)
    {
        var guild = await _db.Q<GuildEntity>().FirstOrDefaultAsync(g => g.Id == id);
        if (guild == null)
        {
            return BerryError("公会不存在");
        }
        return BerryOk(guild);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateGuildAsync([FromBody] GuildEntity guild)
    {
        if (guild == null || guild.Id == Guid.Empty)
        {
            return BerryError("无效的公会信息");
        }
        var existingGuild = await _db.Q<GuildEntity>().FirstOrDefaultAsync(g => g.Id == guild.Id);
        if (existingGuild == null)
        {
            return BerryError("公会不存在");
        }
        existingGuild.Name = guild.Name;
        existingGuild.Description = guild.Description;
        existingGuild.Avatar = guild.Avatar;
        await _db.SaveChangesAsync();
        return BerryOk(existingGuild);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGuildAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BerryError("无效的公会ID");
        }
        var guild = await _db.Q<GuildEntity>().FirstOrDefaultAsync(g => g.Id == id);
        if (guild == null)
        {
            return BerryError("公会不存在");
        }
        _db.Remove(guild);
        await _db.SaveChangesAsync();
        return BerryOk();
    }
}
