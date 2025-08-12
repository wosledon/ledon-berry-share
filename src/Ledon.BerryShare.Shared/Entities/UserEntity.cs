using System.ComponentModel.DataAnnotations.Schema;
using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

[Table("Users")]
public class UserEntity : EntityBase
{
    public enum GenderEnum
    {
        Male,
        Female,
        Other
    }

    public enum RoleEnum
    {
        Guest,
        User,
        Admin,
        SuperAdmin
    }

    public string Name { get; set; } = string.Empty;
    public GenderEnum Gender { get; set; } = GenderEnum.Other;
    public string Tel { get; set; } = string.Empty;


    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public RoleEnum Role { get; set; } = RoleEnum.Guest;


    public Guid? GuildId { get; set; }
    public virtual GuildEntity? Guild { get; set; }


    // public virtual ICollection<GiftFlowEntity> GiftFlowTypes { get; set; } = new List<GiftFlowEntity>();

    public Guid? GiftFlowTypeId { get; set; }
    public virtual GiftFlowTypeEntity? GiftFlowType { get; set; }
}
