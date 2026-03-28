using auth_template.Features.Auth.Responses;
using auth_template.Features.Profile.Responses;
using auth_template.Features.Profile.Transfer.Update;
using auth_template.Utilities;

namespace auth_template.Features.Profile.Services.Profile;

public interface IProfileService
{
    Task<LogicResult<ProfileResponse>> UpdateCoreAsync(string id, UpdateCoreProfileDto dto, CancellationToken ct);
    Task<LogicResult<ProfileResponse>> ToggleVisibilityAsync(string id, CancellationToken ct);
    Task<LogicResult<bool>> DeleteProfileAsync(string id, CancellationToken ct);
    Task<LogicResult<ProfileResponse>> GetOwnProfileAsync(CancellationToken ct);
    Task<LogicResult<ProfileResponse>> GetUserProfileAsync(string identifier, CancellationToken ct);
    Task<LogicResult<ProfileResponse>> UpdateAvatarAsync(IFormFile avatarFile, CancellationToken ct);
    Task<LogicResult<UserUpdateResponse>> GetAuditLogsAsync(string id, CancellationToken ct);
}