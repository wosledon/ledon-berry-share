using Ledon.BerryShare.Api.Services;
using Ledon.BerryShare.Shared.Results;
using Ledon.BerryShare.Shared.Entities;
using Ledon.BerryShare.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ledon.BerryShare.Api.Controllers;

public class StatisticsController : ApiControllerBase
{
    private readonly UnitOfWork _db;

    public StatisticsController(UnitOfWork db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取核心KPI数据
    /// </summary>
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpiData([FromQuery] Guid? guildId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var query = _db.Q<GiftFlowOrderEntity>();
        if (guildId.HasValue)
            query = query.Where(o => o.GuildId == guildId.Value);
        if (startDate.HasValue)
            query = query.Where(o => o.OrderAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.OrderAt < endDate.Value.AddDays(1));
        var orderStats = await query
            .GroupBy(o => 1)
            .Select(g => new {
                TotalOrders = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                ActiveGuilds = g.Select(o => o.GuildId).Distinct().Count()
            })
            .FirstOrDefaultAsync();
        var giftFlowStats = await _db.Q<GiftFlowEntity>()
            .Include(f => f.CommissionType)
            .Where(f => !startDate.HasValue || f.FlowAt >= startDate.Value)
            .Where(f => !endDate.HasValue || f.FlowAt < endDate.Value.AddDays(1))
            .GroupBy(f => 1)
            .Select(g => new {
                // 只统计IncludeInTotal=true的Amount
                TotalAmount = g.Where(f => f.CommissionType != null && f.CommissionType.IncludeInTotal).Sum(f => f.Amount),
                // 抽成和税都统计所有Amount
                TotalCommission = g.Sum(f => f.Amount * (f.CommissionType != null ? f.CommissionType.CommissionRate : 0)),
                TotalTax = g.Sum(f => f.Amount * (f.CommissionType != null ? f.CommissionType.TaxRate : 0)),
                ActiveUsers = g.Select(f => f.UserId).Distinct().Count()
            })
            .FirstOrDefaultAsync();
        var totalOrders = orderStats?.TotalOrders ?? 0;
        var totalAmount = orderStats?.TotalAmount ?? 0;
        var giftFlowTotalAmount = giftFlowStats?.TotalAmount ?? 0;
        var totalCommission = giftFlowStats?.TotalCommission ?? 0;
        var totalTax = giftFlowStats?.TotalTax ?? 0;
        var totalFinal = giftFlowTotalAmount - totalCommission - totalTax;
        var activeUsers = giftFlowStats?.ActiveUsers ?? 0;
        var activeGuilds = orderStats?.ActiveGuilds ?? 0;
        var result = new KpiStatisticsResult
        {
            TotalOrders = totalOrders,
            TotalAmount = giftFlowTotalAmount, // KPI总金额用GiftFlowEntity的统计
            TotalCommission = totalCommission,
            TotalTax = totalTax,
            TotalFinal = totalFinal,
            ActiveUsers = activeUsers,
            ActiveGuilds = activeGuilds,
            AvgOrderAmount = totalOrders > 0 ? totalAmount / totalOrders : 0
        };
        Console.WriteLine($"KPI Data: TotalOrders={totalOrders}, TotalAmount={giftFlowTotalAmount}, TotalCommission={totalCommission}, TotalTax={totalTax}, TotalFinal={totalFinal}, ActiveUsers={activeUsers}, ActiveGuilds={activeGuilds}");
        return Ok(new BerryResult<KpiStatisticsResult>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = result
        });
    }

    /// <summary>
    /// 获取分成类型分布数据
    /// </summary>
    [HttpGet("commission-distribution")]
    public async Task<IActionResult> GetCommissionDistribution([FromQuery] Guid? guildId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var query = _db.Q<GiftFlowEntity>();
        if (guildId.HasValue)
            query = query.Where(f => f.GuildId == guildId.Value);
        if (startDate.HasValue)
            query = query.Where(f => f.FlowAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(f => f.FlowAt < endDate.Value.AddDays(1));
        var commissionData = await query
            .Join(_db.Q<CommissionTypeEntity>(),
                f => f.CommissionTypeId,
                c => c.Id,
                (f, c) => new { f.CommissionTypeId, CommissionTypeName = c.Name, f.Amount, c.IncludeInTotal })
            .GroupBy(x => new { x.CommissionTypeId, x.CommissionTypeName })
            .Select(g => new CommissionDistributionResult
            {
                CommissionTypeId = g.Key.CommissionTypeId,
                CommissionTypeName = g.Key.CommissionTypeName,
                TotalAmount = g.Where(x => x.IncludeInTotal).Sum(x => x.Amount),
                Count = g.Count(),
                AvgAmount = g.Where(x => x.IncludeInTotal).Average(x => x.Amount)
            })
            //.OrderByDescending(x => x.TotalAmount)
            .ToListAsync();
        return Ok(new BerryResult<List<CommissionDistributionResult>>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = commissionData.OrderByDescending(x => x.TotalAmount).ToList()
        });
    }

    /// <summary>
    /// 获取流水趋势数据
    /// </summary>
    [HttpGet("flow-trend")]
    public async Task<IActionResult> GetFlowTrend([FromQuery] Guid? guildId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string period = "day")
    {
        var query = _db.Q<GiftFlowOrderEntity>();
        if (guildId.HasValue)
            query = query.Where(o => o.GuildId == guildId.Value);
        if (startDate.HasValue)
            query = query.Where(o => o.OrderAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.OrderAt < endDate.Value.AddDays(1));
        var orders = await query
            .Select(o => new { o.Id, o.OrderAt, o.Amount })
            .ToListAsync();
        IEnumerable<FlowTrendResult> trendData = period.ToLower() switch
        {
            "week" => orders.GroupBy(o => new { Year = o.OrderAt.Year, Week = GetWeekOfYear(o.OrderAt) })
                            .Select(g => new FlowTrendResult
                            {
                                Period = $"{g.Key.Year}-W{g.Key.Week:D2}",
                                Date = GetFirstDateOfWeek(g.Key.Year, g.Key.Week),
                                OrderCount = g.Count(),
                                TotalAmount = g.Sum(o => o.Amount),
                                AvgAmount = g.Average(o => o.Amount)
                            })
                            .OrderBy(x => x.Date),
            "month" => orders.GroupBy(o => new { o.OrderAt.Year, o.OrderAt.Month })
                             .Select(g => new FlowTrendResult
                             {
                                 Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                                 Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                                 OrderCount = g.Count(),
                                 TotalAmount = g.Sum(o => o.Amount),
                                 AvgAmount = g.Average(o => o.Amount)
                             })
                             .OrderBy(x => x.Date),
            _ => orders.GroupBy(o => o.OrderAt.Date)
                      .Select(g => new FlowTrendResult
                      {
                          Period = g.Key.ToString("yyyy-MM-dd"),
                          Date = g.Key,
                          OrderCount = g.Count(),
                          TotalAmount = g.Sum(o => o.Amount),
                          AvgAmount = g.Average(o => o.Amount)
                      })
                      .OrderBy(x => x.Date)
        };
        return Ok(new BerryResult<List<FlowTrendResult>>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = trendData.ToList()
        });
    }

    /// <summary>
    /// 获取人员绩效排行
    /// </summary>
    [HttpGet("user-performance")]
    public async Task<IActionResult> GetUserPerformance([FromQuery] Guid? guildId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int top = 20)
    {
        var query = _db.Q<GiftFlowEntity>();
        if (guildId.HasValue)
            query = query.Where(f => f.GuildId == guildId.Value);
        if (startDate.HasValue)
            query = query.Where(f => f.FlowAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(f => f.FlowAt < endDate.Value.AddDays(1));
        var userPerformance = await (
            from f in query
            join u in _db.Q<UserEntity>() on f.UserId equals u.Id
            join c in _db.Q<CommissionTypeEntity>() on f.CommissionTypeId equals c.Id
            group new { f, u, c } by new { f.UserId, u.Name } into g
            select new UserPerformanceResult
            {
                UserId = g.Key.UserId,
                UserName = g.Key.Name,
                TotalAmount = g.Where(x=>x.c.IncludeInTotal).Sum(x => x.f.Amount),
                TotalCommission = g.Sum(x => x.f.Amount * x.c.CommissionRate),
                TotalTax = g.Sum(x => x.f.Amount * x.c.TaxRate),
                FinalAmount = g.Sum(x => x.f.Amount * x.c.CommissionRate - x.f.Amount * x.c.TaxRate),
                OrderCount = g.Count(),
                AvgOrderAmount = g.Where(x=>x.c.IncludeInTotal).Average(x => x.f.Amount)
            })
            //.OrderByDescending(x => x.FinalAmount)
            .Take(top)
            .ToListAsync();
        return Ok(new BerryResult<List<UserPerformanceResult>>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = userPerformance.OrderByDescending(x => x.FinalAmount).ToList()
        });
    }

    /// <summary>
    /// 获取公会对比数据
    /// </summary>
    [HttpGet("guild-comparison")]
    public async Task<IActionResult> GetGuildComparison([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var query = _db.Q<GiftFlowOrderEntity>();
        if (startDate.HasValue)
            query = query.Where(o => o.OrderAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.OrderAt < endDate.Value.AddDays(1));
        var guildData = await (
            from o in query
            join g in _db.Q<GuildEntity>() on o.GuildId equals g.Id
            group o by new { o.GuildId, g.Name } into grp
            select new GuildComparisonResult
            {
                GuildId = grp.Key.GuildId,
                GuildName = grp.Key.Name,
                TotalOrders = grp.Count(),
                TotalAmount = grp.Sum(o => o.Amount),
                AvgOrderAmount = grp.Average(o => o.Amount),
                ActiveUsers = _db.Q<GiftFlowEntity>()
                    .Where(f => f.GuildId == grp.Key.GuildId)
                    .Where(f => !startDate.HasValue || f.FlowAt >= startDate.Value)
                    .Where(f => !endDate.HasValue || f.FlowAt < endDate.Value.AddDays(1))
                    .Select(f => f.UserId)
                    .Distinct()
                    .Count()
            })
            //.OrderByDescending(x => x.TotalAmount)
            .ToListAsync();
        return Ok(new BerryResult<List<GuildComparisonResult>>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = guildData.OrderByDescending(x=>x.TotalAmount).ToList()
        });
    }

    /// <summary>
    /// 获取收支分析数据
    /// </summary>
    [HttpGet("revenue-analysis")]
    public async Task<IActionResult> GetRevenueAnalysis([FromQuery] Guid? guildId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var query = _db.Q<GiftFlowEntity>();
        if (guildId.HasValue)
            query = query.Where(f => f.GuildId == guildId.Value);
        if (startDate.HasValue)
            query = query.Where(f => f.FlowAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(f => f.FlowAt < endDate.Value.AddDays(1));
        var revenueData = await (
            from f in query
            join c in _db.Q<CommissionTypeEntity>() on f.CommissionTypeId equals c.Id
            select new { f.Amount, c.CommissionRate, c.TaxRate, c.IncludeInTotal }
        ).ToListAsync();
        var totalAmount = revenueData.Where(x=>x.IncludeInTotal).Sum(x => x.Amount);
        var totalCommission = revenueData.Sum(x => x.Amount * x.CommissionRate);
        var totalTax = revenueData.Sum(x => x.Amount * x.TaxRate);
        var finalAmount = totalCommission - totalTax;
        var result = new RevenueAnalysisResult
        {
            TotalAmount = totalAmount,
            CommissionAmount = totalCommission,
            TaxAmount = totalTax,
            FinalAmount = finalAmount,
            CommissionRate = totalAmount > 0 ? (totalCommission / totalAmount) * 100 : 0,
            TaxRate = totalAmount > 0 ? (totalTax / totalAmount) * 100 : 0,
            NetRate = totalAmount > 0 ? (finalAmount / totalAmount) * 100 : 0
        };
        return Ok(new BerryResult<RevenueAnalysisResult>
        {
            Code = BerryResult.StatusCodeEnum.Success,
            Data = result
        });
    }

    private static int GetWeekOfYear(DateTime time)
    {
        var day = (int)time.DayOfWeek;
        return (time.DayOfYear - day + 10) / 7;
    }

    private static DateTime GetFirstDateOfWeek(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = (int)jan1.DayOfWeek - (int)DayOfWeek.Monday;
        var firstMonday = jan1.AddDays(-daysOffset);
        return firstMonday.AddDays((weekOfYear - 1) * 7);
    }
}