using System;

namespace Ledon.BerryShare.Front.Services
{
    public class GuildContextService
    {
        public Guid CurrentGuildId { get; private set; } = Guid.Empty;
        public event Action? OnGuildChanged;

        public void SetGuildId(Guid guildId)
        {
            CurrentGuildId = guildId;
            OnGuildChanged?.Invoke();
        }
    }
}
