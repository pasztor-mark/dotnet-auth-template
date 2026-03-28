namespace auth_template.Features.Auth.Transfer;

public class ActivityDto
{
    public string PageKey { get; set; }
    public int Seconds { get; set; }

    public void Deconstruct(out string pageKey, out int seconds)
    {
        pageKey = PageKey;
        seconds = Seconds;
    }
}