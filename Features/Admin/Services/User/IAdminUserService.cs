using auth_template.Features.Admin.Responses;
using auth_template.Utilities;

namespace auth_template.Features.Admin.Services.User;

public interface IAdminUserService
{
    // --- User Management ---
    Task<LogicResult<UserManagementResponse>> GetUserForManagementAsync(string userName);
    Task<LogicResult<bool>> BanUserAsync(string userName, string reason);
    Task<LogicResult<bool>> UnbanUserAsync(string userName);

    // --- Subscription & Financial ---
    Task<LogicResult<bool>> ConfirmSubscriptionPaymentAsync(string userName, Guid targetTierId);
}