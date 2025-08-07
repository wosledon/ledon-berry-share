using System;
using Ledon.BerryShare.Shared.Entities;

namespace Ledon.BerryShare.Shared.Base;

public interface IEntity
{
    Guid Id { get; set; }
}

public abstract class EntityBase : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CreateBy { get; set; } = Guid.Empty;
    public DateTime CreateAt { get; set; } = DateTime.Now;
    public Guid ModifyBy { get; set; } = Guid.Empty;
    public DateTime? ModifyAt { get; set; } = null;

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public override string ToString()
    {
        return $"{GetType().Name} (Id: {Id}, Created By: {CreateBy}, Created At: {CreateAt}, Modified By: {ModifyBy}, Modified At: {ModifyAt}, Is Deleted: {IsDeleted})";
    }
}
