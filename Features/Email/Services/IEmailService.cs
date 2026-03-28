using auth_template.Features.Auth.Responses;
using auth_template.Features.Email.Transfer;
using auth_template.Utilities;

namespace auth_template.Features.Email.Services;

public interface IEmailService
{
    Task<LogicResult<bool>> GetConfirmationEmailAsync();
    Task<LogicResult<bool>> ConfirmEmailAsync(string token, string email);
    Task<LogicResult<bool>> SendForgotPasswordEmailAsync(string email, bool? isAccountRecovery = false);
    Task<LogicResult<bool>> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<LogicResult<bool>> SendEmailChangeAsync(ChangeEmailDto dto, string? userId);
    Task<LogicResult<SelfResponse>> ConfirmEmailChangeAsync(ConfirmEmailChangeDto dto, string? userId);
}