using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class Stream : EntityBase
{
    public Guid UserId { get; set; } = Guid.Empty;
    public User? User { get; set; }
    public Guid GuildId { get; set; } = Guid.Empty;
    public Guild? Guild { get; set; }
    public Guid StreamTypeId { get; set; } = Guid.Empty;
    public StreamType? StreamType { get; set; }
    public DateTime StreamAt { get; set; } = DateTime.Now;
    public decimal Amount { get; set; } = 0.0m;
}
