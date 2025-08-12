using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Querys;

public class GiftFlowOrderQuery : IPaged
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? GuildId { get; set; }

    public DateTime? QueryDate { get; set; }

    public DateTime? StartDate => QueryDate?.Date;
    public DateTime? EndDate => QueryDate?.Date.AddDays(1).AddTicks(-1);

    public GiftFlowOrderQuery()
    {
        PageIndex = 1;
        PageSize = 10;
    }
}
