using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class GuildEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;

    public Guid OwnerId { get; set; } = Guid.Empty;
    public virtual UserEntity? Owner { get; set; }

    public virtual ICollection<UserEntity> Members { get; set; } = new List<UserEntity>();

    public override string ToString()
    {
        return $"{base.ToString()}, Name: {Name}, Description: {Description}, Avatar: {Avatar}, OwnerId: {OwnerId}, Members Count: {Members.Count}";
    }
}
