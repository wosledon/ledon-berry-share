using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Querys;

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
            .Select(c => new Ledon.BerryShare.Shared.Results.CommissionTypeResult {
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
                ModifyAt = c.ModifyAt
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
        existingComposition.Name = composition.Name;
        existingComposition.Description = composition.Description;
        await _db.SaveChangesAsync();
        return BerryOk(existingComposition);
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
        _db.Add(composition);
        await _db.SaveChangesAsync();
        return BerryOk(composition);
    }
}