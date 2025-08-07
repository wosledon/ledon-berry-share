using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Ledon.BerryShare.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Querys;

namespace Ledon.BerryShare.Api.Controllers;

[Authorize]
public class CommissionTypeController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public CommissionTypeController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetCommissionTypeListAsync(CommissionTypeQuery query)
    {
        var compositions = await _db.Q<CommissionTypeEntity>()
            .WhereIf(!string.IsNullOrEmpty(query.Search), q => q.Where(c => c.Name.Contains(query.Search!)))
            .OrderByDescending(c => c.CreateAt)
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
}