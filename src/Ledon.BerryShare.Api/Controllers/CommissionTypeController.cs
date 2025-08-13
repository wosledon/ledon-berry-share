using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Querys;
using Ledon.BerryShare.Shared.Results;

namespace Ledon.BerryShare.Api.Controllers;

public class CommissionTypeController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public CommissionTypeController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetCommissionTypeListAsync([FromQuery]CommissionTypeQuery query)
    {
        var compositions = await _db.Q<CommissionTypeEntity>()
            .Include(c => c.Guild)
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(c => c.Name.Contains(query.Search!)))
            .WhereIf(query.GuildId.HasValue, q => q.Where(c => c.GuildId == query.GuildId!.Value))
            .OrderByDescending(c => c.CreateAt)
            .Select(c => new Ledon.BerryShare.Shared.Results.CommissionTypeResult
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                GuildId = c.GuildId,
                GuildName = c.Guild != null ? c.Guild.Name : string.Empty,
                CommissionRate = c.CommissionRate,
                TaxRate = c.TaxRate,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                CreateAt = c.CreateAt,
                ModifyAt = c.ModifyAt,
                IncludeInTotal = c.IncludeInTotal
            })
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(compositions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommissionTypeByIdAsync(Guid id)
    {
        var composition = await _db.Q<CommissionTypeEntity>().FirstOrDefaultAsync(c => c.Id == id);
        if (composition == null)
        {
            return BerryError("分成类型不存在");
        }
        return BerryOk(composition);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateCommissionTypeAsync([FromBody] CommissionTypeEntity composition)
    {
        if (composition == null || composition.Id == Guid.Empty)
        {
            return BerryError("无效的分成类型信息");
        }
        var existingComposition = await _db.Q<CommissionTypeEntity>().FirstOrDefaultAsync(c => c.Id == composition.Id);
        if (existingComposition == null)
        {
            return BerryError("分成类型不存在");
        }

        // 验证 Guild 是否存在（如果要更改 GuildId）
        if (composition.GuildId != Guid.Empty && composition.GuildId != existingComposition.GuildId)
        {
            var guildExists = await _db.Q<GuildEntity>().AnyAsync(g => g.Id == composition.GuildId);
            if (!guildExists)
            {
                return BerryError("指定的公会不存在");
            }
        }

        existingComposition.Name = composition.Name;
        existingComposition.Description = composition.Description;
        existingComposition.GuildId = composition.GuildId;
        existingComposition.CommissionRate = composition.CommissionRate;
        existingComposition.TaxRate = composition.TaxRate;
        existingComposition.IncludeInTotal = composition.IncludeInTotal;
        existingComposition.ModifyAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return BerryOk();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommissionTypeAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BerryError("无效的分成类型ID");
        }
        var composition = await _db.Q<CommissionTypeEntity>().FirstOrDefaultAsync(c => c.Id == id);
        if (composition == null)
        {
            return BerryError("分成类型不存在");
        }
        _db.Remove(composition);
        await _db.SaveChangesAsync();
        return BerryOk();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCommissionTypeAsync([FromBody] CommissionTypeEntity composition)
    {
        if (composition == null)
        {
            return BerryError("无效的分成类型信息");
        }

        // 验证 Guild 是否存在
        if (composition.GuildId != Guid.Empty)
        {
            var guildExists = await _db.Q<GuildEntity>().AnyAsync(g => g.Id == composition.GuildId);
            if (!guildExists)
            {
                return BerryError("指定的公会不存在");
            }
        }

        _db.Add(composition);
        await _db.SaveChangesAsync();

        // 重新查询以获取关联的Guild信息
        var createdComposition = await _db.Q<CommissionTypeEntity>()
            .Include(c => c.Guild)
            .FirstOrDefaultAsync(c => c.Id == composition.Id);

        var result = new CommissionTypeResult
        {
            Id = createdComposition!.Id,
            Name = createdComposition.Name,
            Description = createdComposition.Description,
            GuildId = createdComposition.GuildId,
            GuildName = createdComposition.Guild?.Name ?? string.Empty,
            CommissionRate = createdComposition.CommissionRate,
            TaxRate = createdComposition.TaxRate,
            IsActive = createdComposition.IsActive,
            IsDeleted = createdComposition.IsDeleted,
            CreateAt = createdComposition.CreateAt,
            ModifyAt = createdComposition.ModifyAt,
            IncludeInTotal = createdComposition.IncludeInTotal
        };

        return BerryOk(result);
    }

    [HttpPost("bind/{giftFlowTypeId}")]
    public async Task<IActionResult> BindCommissionTypesToGiftFlowTypeAsync([FromBody] List<Guid> commissionTypeIds, Guid giftFlowTypeId)
    {
        if (giftFlowTypeId == Guid.Empty)
        {
            return BerryError("无效的流水组成ID");
        }

        if (commissionTypeIds == null || !commissionTypeIds.Any())
        {
            return BerryError("无效的流水类型ID列表");
        }

        // 检查流水组成是否存在
        var giftFlowType = await _db.Q<GiftFlowTypeEntity>().Include(g => g.Compositions).FirstOrDefaultAsync(g => g.Id == giftFlowTypeId);
        if (giftFlowType == null)
        {
            return BerryError("流水组成不存在");
        }

        // 检查所有流水类型是否存在
        var commissionTypes = await _db.Q<CommissionTypeEntity>()
            .Where(c => commissionTypeIds.Contains(c.Id))
            .ToListAsync();

        if (commissionTypes.Count != commissionTypeIds.Count)
        {
            return BerryError("部分流水类型不存在");
        }

        // 清除现有绑定
        giftFlowType.Compositions.Clear();

        // 添加新的绑定
        foreach (var commissionType in commissionTypes)
        {
            giftFlowType.Compositions.Add(commissionType);
        }

        _db.Update(giftFlowType);
        await _db.SaveChangesAsync();
        
        return BerryOk();
    }

    [HttpDelete("unbind/{giftFlowTypeId}")]
    public async Task<IActionResult> UnbindCommissionTypesFromGiftFlowTypeAsync(Guid giftFlowTypeId)
    {
        if (giftFlowTypeId == Guid.Empty)
        {
            return BerryError("无效的流水组成ID");
        }

        var giftFlowType = await _db.Q<GiftFlowTypeEntity>().Include(g => g.Compositions).FirstOrDefaultAsync(g => g.Id == giftFlowTypeId);
        if (giftFlowType == null)
        {
            return BerryError("流水组成不存在");
        }

        giftFlowType.Compositions.Clear();
        _db.Update(giftFlowType);
        await _db.SaveChangesAsync();
        
        return BerryOk();
    }

    [HttpGet("by-giftflowtype/{giftFlowTypeId}")]
    public async Task<IActionResult> GetCommissionTypesByGiftFlowTypeAsync(Guid giftFlowTypeId)
    {
        if (giftFlowTypeId == Guid.Empty)
        {
            return BerryError("无效的流水组成ID");
        }

        var giftFlowType = await _db.Q<GiftFlowTypeEntity>()
            .Include(g => g.Compositions)
            .ThenInclude(c => c.Guild)
            .FirstOrDefaultAsync(g => g.Id == giftFlowTypeId);

        if (giftFlowType == null)
        {
            return BerryError("流水组成不存在");
        }

        var commissionTypes = giftFlowType.Compositions.Select(c => new CommissionTypeResult
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            GuildId = c.GuildId,
            GuildName = c.Guild != null ? c.Guild.Name : string.Empty,
            CommissionRate = c.CommissionRate,
            TaxRate = c.TaxRate,
            IsActive = c.IsActive,
            IsDeleted = c.IsDeleted,
            CreateAt = c.CreateAt,
            ModifyAt = c.ModifyAt,
            IncludeInTotal = c.IncludeInTotal
        }).ToList();

        return BerryOk(commissionTypes);
    }
}