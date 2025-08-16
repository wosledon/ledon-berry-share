using Ledon.BerryShare.Shared.Results;

namespace Ledon.BerryShare.Shared.Results;

public class GiftFlowOrderResult
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    // 流水单包含的流水信息
    public List<GiftFlowResult> GiftFlows { get; set; } = new();

    public Guid GuildId { get; set; }
    // 工会信息
    public string GuildName { get; set; } = string.Empty;
}

public class GiftFlowResult
{
    public Guid Id { get; set; }
    public string FlowNumber { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTime FlowDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    public decimal CommissionAmount => CommissionRate * Amount;

    decimal CommissionRate => CommissionType?.CommissionRate ?? 0;

    public decimal TaxRateAmount => TaxRate * CommissionAmount;

    decimal TaxRate => CommissionType?.TaxRate ?? 0;

    public decimal FinalAmount => CommissionAmount - TaxRateAmount;

    public Guid UserId { get; set; }
    // 关联的用户
    public UserResult? User { get; set; }

    public Guid CommissionTypeId { get; set; }
    // 关联的分成类型
    public CommissionTypeResult? CommissionType { get; set; }

    public Guid? GiftFlowTypeId { get; set; }
    // 关联的流水类型
    public GiftFlowTypeResult? GiftFlowType { get; set; }

}
