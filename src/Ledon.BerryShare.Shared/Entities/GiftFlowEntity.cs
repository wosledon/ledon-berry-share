using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class GiftFlowEntity : EntityBase
{
    public Guid UserId { get; set; } = Guid.Empty;
    public virtual UserEntity? User { get; set; }
    public Guid GuildId { get; set; } = Guid.Empty;
    public virtual GuildEntity? Guild { get; set; }
    public Guid StreamTypeId { get; set; } = Guid.Empty;
    public virtual GiftFlowTypeEntity? StreamType { get; set; }
    public DateTime StreamAt { get; set; } = DateTime.Now;
    public decimal Amount { get; set; } = 0.0m;
}
