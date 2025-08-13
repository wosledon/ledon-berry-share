namespace Ledon.BerryShare.Shared;

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

    public BerryResult() { }
}


public class BerryResult<T> : BerryResult
    where T : new()
{
    public T Data { get; set; } = new T();
    
    public int TotalCount
    {
        get => Data is IPagedList pagedList ? pagedList.TotalCount : 0;
        set
        {
            if (Data is IPagedList pagedList)
            {
                pagedList.TotalCount = value;
            }
        }
    }
}
