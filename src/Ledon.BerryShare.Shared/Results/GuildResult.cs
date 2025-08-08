using System;

namespace Ledon.BerryShare.Shared.Results
{
    public class GuildResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
}
