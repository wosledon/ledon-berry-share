using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

[Table("CommissionTypes")]
public class CommissionTypeEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid GuildId { get; set; } = Guid.Empty;
    public virtual GuildEntity? Guild { get; set; }

    public decimal CommissionRate { get; set; } = 0.0m;

    public decimal TaxRate { get; set; } = 0.0m;
}
