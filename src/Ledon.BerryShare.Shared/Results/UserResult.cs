using System;
using Ledon.BerryShare.Shared.Entities;

namespace Ledon.BerryShare.Shared.Results
{
    public class UserResult
    {
        public Guid Id { get; set; }
        public Guid GuildId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tel { get; set; } = string.Empty;
        public string GuildName { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
}
