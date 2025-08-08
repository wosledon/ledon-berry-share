using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

[Table("GiftFlowTypes")]
public class GiftFlowTypeEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid GuildId { get; set; } = Guid.Empty;
    public virtual GuildEntity? Guild { get; set; }

    public virtual ICollection<CommissionTypeEntity> Compositions { get; set; } = new List<CommissionTypeEntity>();

    public virtual ICollection<GiftFlowEntity> GiftFlows { get; set; } = new List<GiftFlowEntity>();
}
