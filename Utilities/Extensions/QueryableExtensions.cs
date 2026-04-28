using auth_template.Responses;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Utilities.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, PaginationScheme scheme)
    {
        return query
            .Skip(scheme.Skip)
            .Take(scheme.PageSize);
    }

    public static Paged<T> ToPaged<T>(this IQueryable<T> query, PaginationScheme scheme)
    {
        var totalCount = query.Count();
        var data = query.Paginate(scheme).ToList();
        
        return new Paged<T>(data, scheme.Page, scheme.PageSize, totalCount);
    }

    public static Paged<T> ListToPaged<T>(this List<T> list, PaginationScheme scheme)
    {
        var totalCount = list.Count;
        var data = list
            .Skip(scheme.Skip)
            .Take(scheme.PageSize)
            .ToList();
            
        return new Paged<T>(data, scheme.Page, scheme.PageSize, totalCount);
    }

    public static async Task<Paged<T>> ToPagedAsync<T>(this IQueryable<T> query, PaginationScheme scheme,
        CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var data = await query.Paginate(scheme).ToListAsync(ct);
        
        return new Paged<T>(data, scheme.Page, scheme.PageSize, totalCount);
    }
}