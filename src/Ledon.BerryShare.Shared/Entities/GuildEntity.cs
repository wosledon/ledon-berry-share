using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

[Table("Guilds")]
public class GuildEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;

    public virtual ICollection<UserEntity> Members { get; set; } = new List<UserEntity>();

    public virtual ICollection<GiftFlowTypeEntity> GiftFlowTypes { get; set; } = new List<GiftFlowTypeEntity>();

    public virtual ICollection<CommissionTypeEntity> CommissionTypes { get; set; } = new List<CommissionTypeEntity>();

    public virtual ICollection<GiftFlowEntity> GiftFlows { get; set; } = new List<GiftFlowEntity>();

    public override string ToString()
    {
        return $"{base.ToString()}, Name: {Name}, Description: {Description}, Avatar: {Avatar}, Members Count: {Members.Count}";
    }
}
