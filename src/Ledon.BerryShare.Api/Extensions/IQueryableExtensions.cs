using System;
using System.Diagnostics.CodeAnalysis;
using Ledon.BerryShare.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledon.BerryShare.Api.Extensions;

public static class IQueryableExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize)
    {
        var totalCount = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return PagedList<T>.Create(items, totalCount, pageIndex, pageSize);
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Func<IQueryable<T>, IQueryable<T>> predicate)
    {
        return condition ? predicate(source) : source;
    }
}