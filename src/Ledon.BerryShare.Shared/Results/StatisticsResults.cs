namespace Ledon.BerryShare.Shared.Results;

public class KpiStatisticsResult
{
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalFinal { get; set; }
    public int ActiveUsers { get; set; }
    public int ActiveGuilds { get; set; }
    public decimal AvgOrderAmount { get; set; }
}

public class CommissionDistributionResult
{
    public Guid CommissionTypeId { get; set; }
    public string CommissionTypeName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
    public decimal AvgAmount { get; set; }
}

public class FlowTrendResult
{
    public string Period { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AvgAmount { get; set; }
}

public class UserPerformanceResult
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalTax { get; set; }
    public decimal FinalAmount { get; set; }
    public int OrderCount { get; set; }
    public decimal AvgOrderAmount { get; set; }
}

public class GuildComparisonResult
{
    public Guid GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AvgOrderAmount { get; set; }
    public int ActiveUsers { get; set; }
}

public class RevenueAnalysisResult
{
    public decimal TotalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal TaxRate { get; set; }
    public decimal NetRate { get; set; }
}
