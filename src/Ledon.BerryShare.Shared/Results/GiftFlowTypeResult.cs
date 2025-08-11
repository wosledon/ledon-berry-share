using System;

namespace Ledon.BerryShare.Shared.Results
{
    public class GiftFlowTypeResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid GuildId { get; set; }
        public string GuildName { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }

        public string CommissionTypeFlow { get; set; } = string.Empty;

        public List<CommissionTypeResult> CommissionTypes { get; set; } = new List<CommissionTypeResult>();
    }
}
