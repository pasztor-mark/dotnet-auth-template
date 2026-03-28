namespace auth_template.Features.Auth.Utilities.Tokens.Refresh;

public interface IRefreshGenerator
{
    string GenerateRefreshToken();
}