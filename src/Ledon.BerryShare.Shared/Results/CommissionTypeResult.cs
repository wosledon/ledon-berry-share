using System;

namespace Ledon.BerryShare.Shared.Results
{
    public class CommissionTypeResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid GuildId { get; set; }
        public string GuildName { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? ModifyAt { get; set; }

        public bool IncludeInTotal { get; set; }
    }
}
