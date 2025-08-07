using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class Guild : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;

    public Guid OwnerId { get; set; } = Guid.Empty;
    public User? Owner { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();

    public override string ToString()
    {
        return $"{base.ToString()}, Name: {Name}, Description: {Description}, Avatar: {Avatar}, OwnerId: {OwnerId}, Members Count: {Members.Count}";
    }
}
