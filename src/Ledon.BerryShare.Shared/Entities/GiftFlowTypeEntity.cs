using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;
using System.Text.Json.Serialization;

namespace Ledon.BerryShare.Shared.Entities;

[Table("GiftFlowTypes")]
public class GiftFlowTypeEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid GuildId { get; set; } = Guid.Empty;
    [JsonIgnore]
    public virtual GuildEntity? Guild { get; set; }

    [JsonIgnore]
    public virtual ICollection<CommissionTypeEntity> Compositions { get; set; } = new List<CommissionTypeEntity>();

    [JsonIgnore]
    public virtual ICollection<GiftFlowEntity> GiftFlows { get; set; } = new List<GiftFlowEntity>();
}
