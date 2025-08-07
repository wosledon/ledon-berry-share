using System;
using Ledon.BerryShare.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledon.BerryShare.Api.Services;

public class UnitOfWork
{
    private readonly BerryShareDbContext _context;

    public UnitOfWork(BerryShareDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IQueryable<TEntity> Q<TEntity>() where TEntity : class
    {
        return _context.Set<TEntity>().AsQueryable();
    }

    public void Update(object entity)
    {
        _context.Update(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public void UpdateRange(IEnumerable<object> entities)
    {
        _context.UpdateRange(entities);
        foreach (var entity in entities)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public void Add(object entity)
    {
        _context.Add(entity);
    }

    public void AddRange(IEnumerable<object> entities)
    {
        _context.AddRange(entities);
    }

    public void Remove(object entity)
    {
        _context.Remove(entity);
    }

    public void RemoveRange(IEnumerable<object> entities)
    {
        _context.RemoveRange(entities);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}