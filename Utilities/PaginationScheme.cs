namespace auth_template.Utilities;

public class PaginationScheme
{
    private int _pageSize = 12;
    private const int MaxPageSize = 100;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    public int Skip => (Page - 1) * PageSize;

    public PaginationScheme() { }

    public PaginationScheme(int page, int pageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize;
    }

    public void Deconstruct(out int page, out int pageSize, out int skip)
    {
        page = Page;
        pageSize = PageSize;
        skip = Skip;
    }
}