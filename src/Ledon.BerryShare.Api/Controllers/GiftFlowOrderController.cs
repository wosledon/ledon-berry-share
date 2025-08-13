using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Entities;
using Ledon.BerryShare.Shared.Querys;
using Ledon.BerryShare.Shared.Results;
using Ledon.BerryShare.Shared;

namespace Ledon.BerryShare.Api.Controllers;

public class GiftFlowOrderController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public GiftFlowOrderController(UnitOfWork db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取流水单列表（分页）
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetGiftFlowOrderListAsync([FromQuery] GiftFlowOrderQuery query)
    {
        var orders = _db.Q<GiftFlowOrderEntity>()
            .Include(o => o.Guild)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.User)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.CommissionType)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.GiftFlowType)
            .AsQueryable();

        // 应用筛选条件
        if (!string.IsNullOrEmpty(query.Search))
        {
            orders = orders.Where(o => o.Description.Contains(query.Search));
        }

        if (query.GuildId.HasValue)
        {
            orders = orders.Where(o => o.GuildId == query.GuildId.Value);
        }

        if (query.StartDate.HasValue)
        {
            orders = orders.Where(o => o.OrderAt >= query.StartDate.Value.Date);
        }

        if (query.EndDate.HasValue)
        {
            orders = orders.Where(o => o.OrderAt <= query.EndDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var pagedOrders = await orders
            .OrderByDescending(o => o.OrderAt)
            .ToPagedListAsync(query.PageIndex, query.PageSize);

        // 内存中投影
        var pagedResult = new PagedList<GiftFlowOrderResult>
        {
            PageIndex = pagedOrders.PageIndex,
            PageSize = pagedOrders.PageSize,
            TotalCount = pagedOrders.TotalCount
        };
        pagedResult.AddRange(pagedOrders.Select(o => new GiftFlowOrderResult
        {
            Id = o.Id,
            OrderNumber = $"FS{o.OrderAt:yyyyMMddHHmmss}",
            Title = string.IsNullOrEmpty(o.Description) ? $"流水单-{o.OrderAt:yyyy-MM-dd HH:mm}" : o.Description,
            Description = o.Description,
            OrderDate = o.OrderAt,
            TotalAmount = o.Amount,
            CreateTime = o.CreateAt,
            UpdateTime = o.ModifyAt ?? o.CreateAt,
            GuildId = o.GuildId,
            GuildName = o.Guild?.Name ?? string.Empty,
            GiftFlows = o.GiftFlows.Select(f => new GiftFlowResult
            {
                Id = f.Id,
                FlowNumber = $"FS{o.OrderAt:yyyyMMddHHmmss}-{f.Id.ToString().Substring(0, 3)}",
                Amount = f.Amount,
                FlowDate = f.FlowAt,
                Remark = f.Remark ?? string.Empty,
                CreateTime = f.CreateAt,
                UserId = f.UserId,
                CommissionTypeId = f.CommissionTypeId,
                GiftFlowTypeId = f.GiftFlowTypeId,
            }).ToList()
        }));


        return BerryOk(pagedResult);
    }

    /// <summary>
    /// 根据ID获取流水单详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftFlowOrderByIdAsync(Guid id)
    {
        var order = await _db.Q<GiftFlowOrderEntity>()
            .Include(o => o.Guild)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.User)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.CommissionType)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.GiftFlowType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return BerryError("流水单不存在");
        }

        return BerryOk(MapToResult(order));
    }

    /// <summary>
    /// 创建新的流水单
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateGiftFlowOrderAsync([FromBody] GiftFlowOrderResult request)
    {
        if (request == null)
        {
            return BerryError("请求数据不能为空");
        }

        if (request.GiftFlows == null || !request.GiftFlows.Any())
        {
            return BerryError("流水单必须包含至少一条流水记录");
        }

        // 计算总金额
        var totalAmount = request.GiftFlows.Sum(f => f.Amount);

        var order = new GiftFlowOrderEntity
        {
            Description = request.Description ?? string.Empty,
            OrderAt = DateTime.Now,
            Amount = totalAmount,
            GuildId = request.GuildId
        };

        _db.Add(order);
        await _db.SaveChangesAsync();

        // 创建流水记录
        var giftFlows = request.GiftFlows.Select(f => new GiftFlowEntity
        {
            OrderId = order.Id,
            UserId = f.UserId,
            GuildId = order.GuildId,
            CommissionTypeId = f.CommissionTypeId,
            GiftFlowTypeId = f.GiftFlowTypeId,
            Amount = f.Amount,
            FlowAt = DateTime.Now,
            Remark = f.Remark
        }).ToList();

        _db.AddRange(giftFlows);
        await _db.SaveChangesAsync();

        // 重新查询完整数据返回
        var createdOrder = await _db.Q<GiftFlowOrderEntity>()
            .Include(o => o.Guild)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.User)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.CommissionType)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.GiftFlowType)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        return BerryOk(MapToResult(createdOrder!));
    }

    /// <summary>
    /// 更新流水单
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> UpdateGiftFlowOrderAsync([FromBody] GiftFlowOrderResult request)
    {
        if (request?.Id == null || request.Id == Guid.Empty)
        {
            return BerryError("无效的流水单ID");
        }

        var order = await _db.Q<GiftFlowOrderEntity>()
            .Include(o => o.GiftFlows)
            .FirstOrDefaultAsync(o => o.Id == request.Id);

        if (order == null)
        {
            return BerryError("流水单不存在");
        }

        // 更新基本信息
        order.Description = request.Description ?? order.Description;
        
        order.OrderAt = DateTime.Now; // 更新为当前时间

        // 如果提供了新的流水记录，先删除旧的再添加新的
        if (request.GiftFlows != null)
        {
            // 删除现有流水记录
            _db.RemoveRange(order.GiftFlows);

            // 添加新的流水记录
            var newGiftFlows = request.GiftFlows.Select(f => new GiftFlowEntity
            {
                OrderId = order.Id,
                UserId = f.UserId,
                GuildId = order.GuildId,
                CommissionTypeId = f.CommissionTypeId,
                GiftFlowTypeId = f.GiftFlowTypeId,
                Amount = f.Amount,
                FlowAt = DateTime.Now,
                Remark = f.Remark
            }).ToList();

            _db.AddRange(newGiftFlows);

            // 重新计算总金额
            order.Amount = newGiftFlows.Sum(f => f.Amount);
        }

        await _db.SaveChangesAsync();

        // 重新查询完整数据返回
        var updatedOrder = await _db.Q<GiftFlowOrderEntity>()
            .Include(o => o.Guild)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.User)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.CommissionType)
            .Include(o => o.GiftFlows)
                .ThenInclude(f => f.GiftFlowType)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        return BerryOk(MapToResult(updatedOrder!));
    }

    /// <summary>
    /// 删除流水单
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGiftFlowOrderAsync(Guid id)
    {
        var order = await _db.Q<GiftFlowOrderEntity>()
            .Include(o => o.GiftFlows)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return BerryError("流水单不存在");
        }

        // 删除所有关联的流水记录
        _db.RemoveRange(order.GiftFlows);
        
        // 删除流水单
        _db.Remove(order);
        
        await _db.SaveChangesAsync();

        return BerryOk("流水单删除成功");
    }

    /// <summary>
    /// 根据用户ID获取流水信息
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserFlowsAsync(Guid userId, [FromQuery] GiftFlowOrderQuery query)
    {
        var flows = _db.Q<GiftFlowEntity>()
            .Include(f => f.User)
            .Include(f => f.CommissionType)
            .Include(f => f.GiftFlowType)
            .Include(f => f.Order)
            .Where(f => f.UserId == userId);

        if (query.GuildId.HasValue)
        {
            flows = flows.Where(f => f.GuildId == query.GuildId.Value);
        }

        if (query.StartDate.HasValue)
        {
            flows = flows.Where(f => f.FlowAt >= query.StartDate.Value.Date);
        }

        if (query.EndDate.HasValue)
        {
            flows = flows.Where(f => f.FlowAt <= query.EndDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var pagedFlows = await flows
            .OrderByDescending(f => f.FlowAt)
            .ToPagedListAsync(query.PageIndex, query.PageSize);

        var result = new PagedList<GiftFlowResult>
        {
            PageIndex = pagedFlows.PageIndex,
            PageSize = pagedFlows.PageSize,
            TotalCount = pagedFlows.TotalCount
        };
        result.AddRange(pagedFlows.Select(entity => MapGiftFlowToResult(entity)));

        return BerryOk(result);
    }

    /// <summary>
    /// 实体映射到结果对象
    /// </summary>
    private GiftFlowOrderResult MapToResult(GiftFlowOrderEntity entity)
    {
        return new GiftFlowOrderResult
        {
            Id = entity.Id,
            OrderNumber = $"ORD-{entity.Id.ToString()[..8].ToUpper()}", // 生成订单号
            Title = entity.Description, // 使用Description作为Title
            Description = entity.Description,
            OrderDate = entity.OrderAt,
            TotalAmount = entity.Amount,
            CreateTime = entity.CreateAt,
            UpdateTime = entity.ModifyAt ?? entity.CreateAt,
            
            GiftFlows = entity.GiftFlows?.Select(MapGiftFlowToResult).ToList() ?? new List<GiftFlowResult>()
        };
    }

    /// <summary>
    /// 流水实体映射到结果对象
    /// </summary>
    private GiftFlowResult MapGiftFlowToResult(GiftFlowEntity entity)
    {
        return new GiftFlowResult
        {
            Id = entity.Id,
            FlowNumber = $"FLOW-{entity.Id.ToString()[..8].ToUpper()}",
            Amount = entity.Amount,
            FlowDate = entity.FlowAt,
            Remark = entity.Remark,
            CreateTime = entity.CreateAt,
            UpdateTime = entity.ModifyAt ?? entity.CreateAt,
            User = entity.User == null ? null : new UserResult
            {
                Id = entity.User.Id,
                Name = entity.User.Name,
                Tel = entity.User.Tel,
                GuildId = entity.User.GuildId ?? Guid.Empty,
                CreateAt = entity.User.CreateAt
            },
            CommissionType = entity.CommissionType == null ? null : new CommissionTypeResult
            {
                Id = entity.CommissionType.Id,
                Name = entity.CommissionType.Name,
                CommissionRate = entity.CommissionType.CommissionRate,
                TaxRate = entity.CommissionType.TaxRate,
                Description = entity.CommissionType.Description,
                GuildId = entity.CommissionType.GuildId,
                CreateAt = entity.CommissionType.CreateAt,
                ModifyAt = entity.CommissionType.ModifyAt,
                IncludeInTotal = entity.CommissionType.IncludeInTotal
            },
            GiftFlowType = entity.GiftFlowType == null ? null : new GiftFlowTypeResult
            {
                Id = entity.GiftFlowType.Id,
                Name = entity.GiftFlowType.Name,
                Description = entity.GiftFlowType.Description,
                GuildId = entity.GiftFlowType.GuildId,
                CreateAt = entity.GiftFlowType.CreateAt
            }
        };
    }
}


// public class GiftFlowController : ApiControllerBase
// {
//     private readonly UnitOfWork _db;

//     public GiftFlowController(UnitOfWork db)
//     {
//         _db = db;
//     }

//     [HttpGet("list")]
//     public async Task<IActionResult> GetGiftFlowListAsync([FromQuery] GiftFlowQuery query)
//     {
//         var flows = await _db.Q<GiftFlowEntity>()
//             .WhereIf(query.GuildId.HasValue, q => q.Where(s => s.GuildId == query.GuildId!.Value))
//             .WhereIf(query.UserId.HasValue, q => q.Where(s => s.UserId == query.UserId!.Value))
//             .WhereIf(query.StreamTypeId.HasValue, q => q.Where(s => s.StreamTypeId == query.StreamTypeId!.Value))
//             .WhereIf(query.Day.HasValue, q => q.Where(s =>
//                 s.StreamAt >= query.Day!.Value.Date &&
//                 s.StreamAt < query.Day!.Value.Date.AddDays(1)
//             )).OrderByDescending(s => s.CreateAt)
//             .ToPagedListAsync(query.PageIndex, query.PageSize);
//         return BerryOk(flows);
//     }

//     [HttpGet("{id}")]
//     public async Task<IActionResult> GetGiftFlowByIdAsync(Guid id)
//     {
//         var flow = await _db.Q<GiftFlowEntity>().FirstOrDefaultAsync(s => s.Id == id);
//         if (flow == null)
//         {
//             return BerryError("流不存在");
//         }
//         return BerryOk(flow);
//     }

//     [HttpPut("update")]
//     public async Task<IActionResult> UpdateGiftFlowAsync([FromBody] GiftFlowEntity flow)
//     {
//         if (flow == null || flow.Id == Guid.Empty)
//         {
//             return BerryError("无效的流信息");
//         }
//         var existingFlow = await _db.Q<GiftFlowEntity>().FirstOrDefaultAsync(s => s.Id == flow.Id);
//         if (existingFlow == null)
//         {
//             return BerryError("流不存在");
//         }
//         existingFlow.StreamAt = flow.StreamAt;
//         existingFlow.Amount = flow.Amount;
//         await _db.SaveChangesAsync();
//         return BerryOk(existingFlow);
//     }

//     [HttpDelete("{id}")]
//     public async Task<IActionResult> DeleteGiftFlowAsync(Guid id)
//     {
//         if (id == Guid.Empty)
//         {
//             return BerryError("无效的流ID");
//         }
//         var flow = await _db.Q<GiftFlowEntity>().FirstOrDefaultAsync(s => s.Id == id);
//         if (flow == null)
//         {
//             return BerryError("流不存在");
//         }
//         _db.Remove(flow);
//         await _db.SaveChangesAsync();
//         return BerryOk();
//     }

//     [HttpPost("create")]
//     public async Task<IActionResult> CreateGiftFlowAsync([FromBody] List<GiftFlowEntity> flows)
//     {
//         if (flows == null || !flows.Any())
//         {
//             return BerryError("无效的流水信息");
//         }

//         _db.AddRange(flows);
//         await _db.SaveChangesAsync();
//         return BerryOk(flows);
//     }
// }
