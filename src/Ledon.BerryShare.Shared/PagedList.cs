namespace Ledon.BerryShare.Shared;

public interface IPagedList
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; }
}

public class PagedList<T> : List<T>, IPagedList
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedList<T> Create(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        return new PagedList<T>(items, totalCount, pageIndex, pageSize);
    }

    private PagedList(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        AddRange(items);
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }

    public PagedList()
    {
        PageIndex = 1;
        PageSize = 10;
        TotalCount = 0;
    }
}


