using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class StreamType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid GuildId { get; set; } = Guid.Empty;
    public Guild? Guild { get; set; }

    public ICollection<StreamComposition> Compositions { get; set; } = new List<StreamComposition>();
}
