using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

[Table("GiftFlows")]
public class GiftFlowEntity : EntityBase
{
    public Guid UserId { get; set; } = Guid.Empty;
    public virtual UserEntity? User { get; set; }
    public Guid GuildId { get; set; } = Guid.Empty;
    public virtual GuildEntity? Guild { get; set; }
    public Guid CommissionTypeId { get; set; } = Guid.Empty;
    public virtual CommissionTypeEntity? CommissionType { get; set; }
    public Guid? GiftFlowTypeId { get; set; }
    public virtual GiftFlowTypeEntity? GiftFlowType { get; set; }
    public Guid? OrderId { get; set; }
    public virtual GiftFlowOrderEntity? Order { get; set; }
    public DateTime FlowAt { get; set; } = DateTime.Now;
    public decimal Amount { get; set; } = 0.0m;
    public string? Remark { get; set; }
}
