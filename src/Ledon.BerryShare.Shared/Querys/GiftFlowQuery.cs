using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Querys;

public class GiftFlowQuery : IPaged
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? UserId { get; set; }
    public Guid? GuildId { get; set; }
    public Guid? StreamTypeId { get; set; }

    public DateTime? Day { get; set; }

    public GiftFlowQuery()
    {
        PageIndex = 1;
        PageSize = 10;
    }
}
