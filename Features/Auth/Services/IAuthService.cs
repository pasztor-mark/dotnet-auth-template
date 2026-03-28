using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Transfer;
using auth_template.Utilities;

namespace auth_template.Features.Auth.Services;

public interface IAuthService
{
    Task<LogicResult<SelfResponse>> RegisterUserAsync(RegisterDto registerDto, string? userAgent, string? ipAddress);
    Task<LogicResult<SelfResponse>> LoginUserAsync(LoginDto dto, string? userAgent, string? ipAddress);
    Task<LogicResult<SelfResponse>> GetSelfAsync(string? userId);
    Task<LogicResult<bool>> LogoutFromDeviceAsync();
    Task<LogicResult<bool>> LogoutFromAllDevicesAsync();
    Task<LogicResult<bool>> CheckEmailAvailabilityAsync(string? emailAddress);
    Task<LogicResult<bool>> CheckUsernameAvailabilityAsync(string? userName);
    Task<LogicResult<PreferenceResponse>> GetPreferencesAsync(string? userId);
    Task<LogicResult<bool>> ChangePasswordAsync(ChangePasswordDto dto, string? userId);
    Task<LogicResult<bool>> ChangeUsernameAsync(ChangeUsernameDto dto, string? userId);
    void PostHeartbeat(string? userId, ActivityDto dto);
    Task<LogicResult<bool>> FlagUserForAnonymizationAsync(string? userId);
    Task<LogicResult<bool>> RecoverFlaggedUserAsync(ReactivateAccountDto dto);
    Task<LogicResult<bool>> RefreshAsync();
    Task<LogicResult<List<TagListingResponse>>> SearchTagsAsync(string query);
    Task<LogicResult<SelfResponse>> RegisterAdminAsync(RegisterDto registerDto, string? userAgent, string? ipAddress);
}