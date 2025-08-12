using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

[Table("GiftFlowOrder")]
public class GiftFlowOrderEntity : EntityBase
{
    public DateTime OrderAt { get; set; } = DateTime.Now;

    public string Description { get; set; } = string.Empty;

    public Guid GuildId { get; set; } = Guid.Empty;
    public virtual GuildEntity? Guild { get; set; }

    /// <summary>
    /// 总流水
    /// </summary>
    /// <value></value>
    public decimal Amount { get; set; } = 0.0m;

    public virtual ICollection<GiftFlowEntity> GiftFlows { get; set; } = new List<GiftFlowEntity>();
}
