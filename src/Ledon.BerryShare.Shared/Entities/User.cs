using Ledon.BerryShare.Shared.Base;

namespace Ledon.BerryShare.Shared.Entities;

public class User : EntityBase
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
}
