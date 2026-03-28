using auth_template.Utilities;

namespace auth_template.Responses;

public class Paged<T>
{
    public Paged(List<T>? data, int? page, int? pageSize)
    {
        this.data = data ?? [];
        Page = page ?? 1;
        ItemCount = data?.Count ?? 0;
        PageSize = pageSize ?? 0;
    }
    public Paged() {}

    public List<T> data { get; set; }
    public int Page { get; set; }
    public int ItemCount { get; set; }
    public int PageSize { get; set; }

    
    public Paged(List<T> data, PaginationScheme pagination)
    {
        this.data = data;
        this.ItemCount = data.Count;
        this.Page = pagination.page;
        this.PageSize = pagination.pageSize;
    }


}
