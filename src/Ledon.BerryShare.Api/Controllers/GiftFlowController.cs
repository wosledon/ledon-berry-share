using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Entities;
using Ledon.BerryShare.Shared.Querys;

namespace Ledon.BerryShare.Api.Controllers;

[Authorize]
public class GiftFlowController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public GiftFlowController(UnitOfWork db)
    {
        _db = db;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetGiftFlowListAsync(GiftFlowQuery query)
    {
        var flows = await _db.Q<GiftFlowEntity>()
            .WhereIf(query.GuildId.HasValue, q => q.Where(s => s.GuildId == query.GuildId!.Value))
            .WhereIf(query.UserId.HasValue, q => q.Where(s => s.UserId == query.UserId!.Value))
            .WhereIf(query.StreamTypeId.HasValue, q => q.Where(s => s.StreamTypeId == query.StreamTypeId!.Value))
            .WhereIf(query.Day.HasValue, q => q.Where(s =>
                s.StreamAt >= query.Day!.Value.Date &&
                s.StreamAt < query.Day!.Value.Date.AddDays(1)
            )).OrderByDescending(s => s.CreateAt)
            .ToPagedListAsync(query.PageIndex, query.PageSize);
        return BerryOk(flows);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftFlowByIdAsync(Guid id)
    {
        var flow = await _db.Q<GiftFlowEntity>().FirstOrDefaultAsync(s => s.Id == id);
        if (flow == null)
        {
            return BerryError("流不存在");
        }
        return BerryOk(flow);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateGiftFlowAsync([FromBody] GiftFlowEntity flow)
    {
        if (flow == null || flow.Id == Guid.Empty)
        {
            return BerryError("无效的流信息");
        }
        var existingFlow = await _db.Q<GiftFlowEntity>().FirstOrDefaultAsync(s => s.Id == flow.Id);
        if (existingFlow == null)
        {
            return BerryError("流不存在");
        }
        existingFlow.StreamAt = flow.StreamAt;
        existingFlow.Amount = flow.Amount;
        await _db.SaveChangesAsync();
        return BerryOk(existingFlow);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGiftFlowAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BerryError("无效的流ID");
        }
        var flow = await _db.Q<GiftFlowEntity>().FirstOrDefaultAsync(s => s.Id == id);
        if (flow == null)
        {
            return BerryError("流不存在");
        }
        _db.Remove(flow);
        await _db.SaveChangesAsync();
        return BerryOk();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateGiftFlowAsync([FromBody] List<GiftFlowEntity> flows)
    {
        if (flows == null || !flows.Any())
        {
            return BerryError("无效的流水信息");
        }

        _db.AddRange(flows);
        await _db.SaveChangesAsync();
        return BerryOk(flows);
    }
}
