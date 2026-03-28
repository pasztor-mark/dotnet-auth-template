namespace auth_template.Features.Auth.Utilities.Tokens.Jwt;

public interface ITokenUtility
{
    void SetTokenCookies(string accessToken, string? refreshToken);
    bool TokenIsExpired(string token);
    void ClearTokens(bool clearRefresh = false, bool clearAccess = false);
}