using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Querys;

namespace Ledon.BerryShare.Api.Controllers;

[Authorize]
public class GiftFlowTypeController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public GiftFlowTypeController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetGiftFlowTypeListAsync(GiftFlowTypeQuery query)
    {
        var types = await _db.Q<GiftFlowTypeEntity>()
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(t => t.Name.Contains(query.Search!)))
            .WhereIf(query.GuildId.HasValue, q => q.Where(t => t.GuildId == query.GuildId!.Value))
            .OrderByDescending(t => t.CreateAt)
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(types);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftFlowTypeByIdAsync(Guid id)
    {
        var type = await _db.Q<GiftFlowTypeEntity>().FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return BerryError("流水类型不存在");
        }
        return BerryOk(type);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateGiftFlowTypeAsync([FromBody] GiftFlowTypeEntity type)
    {
        if (type == null || type.Id == Guid.Empty)
        {
            return BerryError("无效的流水类型信息");
        }
        var existingType = await _db.Q<GiftFlowTypeEntity>().FirstOrDefaultAsync(t => t.Id == type.Id);
        if (existingType == null)
        {
            return BerryError("流水类型不存在");
        }
        existingType.Name = type.Name;
        existingType.Description = type.Description;
        await _db.SaveChangesAsync();
        return BerryOk(existingType);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGiftFlowTypeAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BerryError("无效的流水类型ID");
        }
        var type = await _db.Q<GiftFlowTypeEntity>().FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return BerryError("流水类型不存在");
        }
        _db.Remove(type);
        await _db.SaveChangesAsync();
        return BerryOk();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateGiftFlowTypeAsync([FromBody] GiftFlowTypeEntity type)
    {
        if (type == null)
        {
            return BerryError("无效的流水类型信息");
        }
        _db.Add(type);
        await _db.SaveChangesAsync();
        return BerryOk(type);
    }

    [HttpPost("commissions/{id}")]
    public async Task<IActionResult> BindGiftFlowTypeAsync([FromBody] List<CommissionTypeEntity> compositions, Guid id)
    {
        if (id == Guid.Empty)
        {
            return BerryError("无效的流水类型ID");
        }

        if (compositions == null || !compositions.Any())
        {
            return BerryError("无效的分成组合信息");
        }

        var type = await _db.Q<GiftFlowTypeEntity>().FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return BerryError("流水类型不存在");
        }

        foreach (var composition in compositions)
        {
            var existingComposition = await _db.Q<CommissionTypeEntity>().FirstOrDefaultAsync(c => c.Id == composition.Id);
            if (existingComposition == null)
            {
                return BerryError($"分成组合 {composition.Name} 不存在");
            }
        }

        type.Compositions = compositions;
        _db.Update(type);
        await _db.SaveChangesAsync();
        return BerryOk();
    }
}
