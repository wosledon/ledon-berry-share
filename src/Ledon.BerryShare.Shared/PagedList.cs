namespace Ledon.BerryShare.Shared;

public class PagedList<T> : List<T>
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
}

public class BerryResult
{
    public enum StatusCodeEnum
    {
        Success = 200,
        Error = 500,
        NotFound = 404,
        BadRequest = 400,
        Unauthorized = 401
    }

    public StatusCodeEnum Code { get; set; } = StatusCodeEnum.Success;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; } = null;

    public BerryResult() { }
}


public class BerryResult<T> : BerryResult
    where T : new()
{
    public new T Data { get; set; } = new T();
}

