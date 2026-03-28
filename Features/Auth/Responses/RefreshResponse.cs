namespace auth_template.Features.Auth.Responses;

public class RefreshResponse
{
    public string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}