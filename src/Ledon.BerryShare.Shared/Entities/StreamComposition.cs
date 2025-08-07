using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class StreamComposition : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid GuildId { get; set; } = Guid.Empty;
    public Guild? Guild { get; set; }

    public decimal CommissionRate { get; set; } = 0.0m;

    public decimal TaxRate { get; set; } = 0.0m;
}
