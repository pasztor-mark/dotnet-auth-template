namespace auth_template.Utilities;

public class PaginationScheme
{
    public PaginationScheme()
    {
    }
    public PaginationScheme(int page, int pageSize)
    {
        this.page = page;
        this.pageSize = pageSize;
    }

    public int page { get; set; } = 1;
    public int pageSize { get; set; } = 12;

    public void Deconstruct(out int page, out int pageSize)
    {
        page = this.page;
        pageSize = this.pageSize;
    }
}