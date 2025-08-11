using Microsoft.EntityFrameworkCore;
using Ledon.BerryShare.Shared.Base;
using System.Reflection;
using System.Linq.Expressions;

namespace Ledon.BerryShare.Api.Data;

public class BerryShareDbContext : DbContext
{
    public BerryShareDbContext(DbContextOptions<BerryShareDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 自动注册所有继承自 IEntity 的非抽象类
        var entityType = typeof(IEntity);
        var assembly = Assembly.Load("Ledon.BerryShare.Shared");
        var types = assembly.GetTypes()
            .Where(t => entityType.IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
        foreach (var type in types)
        {
            var entity = modelBuilder.Entity(type);
            // 软删除全局查询过滤器
            var isDeletedProp = type.GetProperty("IsDeleted");
            if (isDeletedProp != null && isDeletedProp.PropertyType == typeof(bool))
            {
                var parameter = Expression.Parameter(type, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, isDeletedProp),
                    Expression.Constant(false)
                );
                var lambda = Expression.Lambda(body, parameter);
                entity.HasQueryFilter(lambda);
            }
        }
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ledon.BerryShare.Shared.Entities.GuildEntity>().HasData(new List<Ledon.BerryShare.Shared.Entities.GuildEntity>()
        {
            new Ledon.BerryShare.Shared.Entities.GuildEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "公会1"
            },
            new Ledon.BerryShare.Shared.Entities.GuildEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "公会2"
            }
        });

        modelBuilder.Entity<Ledon.BerryShare.Shared.Entities.CommissionTypeEntity>().HasData(new List<Ledon.BerryShare.Shared.Entities.CommissionTypeEntity>()
        {
            new Ledon.BerryShare.Shared.Entities.CommissionTypeEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "普通礼物流水",
                CommissionRate = 0.5m,
                TaxRate = 0.04m,
                Description = "普通礼物流水描述",
                GuildId = Guid.Parse("00000000-0000-0000-0000-000000000001")
            },
            new Ledon.BerryShare.Shared.Entities.CommissionTypeEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "幸运礼物流水",
                CommissionRate = 0.5m,
                TaxRate = 0.02m,
                Description = "幸运礼物流水描述",
                GuildId = Guid.Parse("00000000-0000-0000-0000-000000000001")
            },
            new Ledon.BerryShare.Shared.Entities.CommissionTypeEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "管理员抽成",
                CommissionRate = 0.1m,
                TaxRate = 0.04m,
                Description = "管理员抽成描述",
                GuildId = Guid.Parse("00000000-0000-0000-0000-000000000001")
            }
        });

        // 添加UserEntity种子数据
        modelBuilder.Entity<Ledon.BerryShare.Shared.Entities.UserEntity>().HasData(new List<Ledon.BerryShare.Shared.Entities.UserEntity>()
        {
            new Ledon.BerryShare.Shared.Entities.UserEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "超级管理员",
                Account = "super",
                Password = "123456",
                Tel = "12345678901",
                Role = Ledon.BerryShare.Shared.Entities.UserEntity.RoleEnum.SuperAdmin
            },
            new Ledon.BerryShare.Shared.Entities.UserEntity
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "饺子",
                Account = "user",
                Password = "123456",
                Tel = "12345678901",
                GuildId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Role = Ledon.BerryShare.Shared.Entities.UserEntity.RoleEnum.User
            }
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditAndSoftDelete()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IEntity && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));
        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var type = entity.GetType();

            // 审计字段自动赋值
            var now = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
            {
                var createdProp = type.GetProperty("CreatedAt");
                if (createdProp != null && createdProp.PropertyType == typeof(DateTime))
                    createdProp.SetValue(entity, now);
            }
            if (entry.State == EntityState.Modified)
            {
                var updatedProp = type.GetProperty("UpdatedAt");
                if (updatedProp != null && updatedProp.PropertyType == typeof(DateTime))
                    updatedProp.SetValue(entity, now);
            }

            // 软删除处理
            var isDeletedProp = type.GetProperty("IsDeleted");
            if (entry.State == EntityState.Deleted && isDeletedProp != null && isDeletedProp.PropertyType == typeof(bool))
            {
                isDeletedProp.SetValue(entity, true);
                entry.State = EntityState.Modified;
            }
        }
    }
}
