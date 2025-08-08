using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Querys;

public class CommissionTypeQuery : IPaged
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }

    public Guid? GuildId { get; set; }

    public CommissionTypeQuery()
    {
        PageIndex = 1;
        PageSize = 10;
    }
}
