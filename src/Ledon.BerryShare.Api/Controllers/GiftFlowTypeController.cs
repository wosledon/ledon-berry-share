using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Querys;
using Castle.Components.DictionaryAdapter.Xml;

namespace Ledon.BerryShare.Api.Controllers;

public class GiftFlowTypeController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public GiftFlowTypeController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetGiftFlowTypeListAsync([FromQuery]GiftFlowTypeQuery query)
    {
        var types = await _db.Q<GiftFlowTypeEntity>()
            .Include(t => t.Guild)
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(t => t.Name.Contains(query.Search!)))
            .WhereIf(query.GuildId.HasValue, q => q.Where(t => t.GuildId == query.GuildId!.Value))
            .OrderByDescending(t => t.CreateAt)
            .Select(t => new Ledon.BerryShare.Shared.Results.GiftFlowTypeResult
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                GuildId = t.GuildId,
                GuildName = t.Guild != null ? t.Guild.Name : string.Empty,
                CreateAt = t.CreateAt,
                CommissionTypeFlow = string.Join("->", t.Compositions.Select(c => c.Name)),
            })
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(types);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftFlowTypeByIdAsync(Guid id)
    {
        var type = await _db.Q<GiftFlowTypeEntity>().Include(t => t.Guild).FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return BerryError("流水类型不存在");
        }
        var dto = new Ledon.BerryShare.Shared.Results.GiftFlowTypeResult
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            GuildId = type.GuildId,
            GuildName = type.Guild != null ? type.Guild.Name : string.Empty,
            CreateAt = type.CreateAt,
            CommissionTypes = type.Compositions.Select(c => new Ledon.BerryShare.Shared.Results.CommissionTypeResult
            {
                Id = c.Id,
                Name = c.Name,
                CommissionRate = c.CommissionRate,
                TaxRate = c.TaxRate,
                Description = c.Description,
                CreateAt = c.CreateAt,
                IncludeInTotal = c.IncludeInTotal
            }).ToList()
        };
        return BerryOk(dto);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateGiftFlowTypeAsync([FromBody] GiftFlowTypeEntity type)
    {
        if (type == null || type.Id == Guid.Empty)
        {
            return BerryError("无效的流水类型信息");
        }
        var existingType = await _db.Q<GiftFlowTypeEntity>().Include(t => t.Guild).FirstOrDefaultAsync(t => t.Id == type.Id);
        if (existingType == null)
        {
            return BerryError("流水类型不存在");
        }
        existingType.Name = type.Name;
        existingType.Description = type.Description;
        existingType.GuildId = type.GuildId;
        await _db.SaveChangesAsync();
        var dto = new Ledon.BerryShare.Shared.Results.GiftFlowTypeResult {
            Id = existingType.Id,
            Name = existingType.Name,
            Description = existingType.Description,
            GuildId = existingType.GuildId,
            GuildName = existingType.Guild != null ? existingType.Guild.Name : string.Empty,
            CreateAt = existingType.CreateAt
        };
        return BerryOk(dto);
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
        var entity = await _db.Q<GiftFlowTypeEntity>().Include(t => t.Guild).FirstOrDefaultAsync(t => t.Id == type.Id);

        if (entity == null)
        {
            return BerryError("流水类型不存在");
        }

        var dto = new Ledon.BerryShare.Shared.Results.GiftFlowTypeResult {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            GuildId = entity.GuildId,
            GuildName = entity.Guild != null ? entity.Guild.Name : string.Empty,
            CreateAt = entity.CreateAt
        };
        return BerryOk(dto);
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
