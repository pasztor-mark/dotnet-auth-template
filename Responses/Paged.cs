
namespace auth_template.Responses;

public class Paged<T>
{
    public Paged(List<T>? data, int page, int pageSize, int totalCount)
    {
        Data = data ?? [];
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        ItemCount = Data.Count;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public Paged() {}

    public List<T> Data { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int ItemCount { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}