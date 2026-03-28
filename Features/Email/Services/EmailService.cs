using System.Linq.Expressions;
using System.Text.RegularExpressions;
using auth_template.Configuration;
using auth_template.Entities;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Enums;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Utilities.Permissions;
using auth_template.Features.Auth.Utilities.User;
using auth_template.Features.Email.Enums;
using auth_template.Features.Email.Transfer;
using auth_template.Features.Email.Utilities;
using auth_template.Features.Email.Utilities.Client;
using auth_template.Options;
using auth_template.Utilities;
using auth_template.Utilities.Audit;
using auth_template.Utilities.Security.Encryption;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace auth_template.Features.Email.Services;

public class EmailService(
    AppDbContext _ctx,
    IEmailSenderClient _client,
    IAuditUtility _audit,
    IUserUtils _userUtils,
    UserManager<AppUser> _userManager,
    IEncryptor _encryptor,
    IOptions<DatabaseOptions> _opt,
    IPermissionUtility _perms,
    ILogger<EmailService> _logger) : IEmailService
{
    private readonly string apiUrl = _opt.Value.ApiUrl;

    public async Task<LogicResult<bool>> GetConfirmationEmailAsync()
    {
        AppUser? user = await _userUtils.GetCurrentUser();

        if (user is null) return LogicResult<bool>.NotFound("User not found");

        if (user.EmailConfirmed) return LogicResult<bool>.Conflict("E-mail is already confirmed");

        string email = user.NormalizedEmail;
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontendUrl = Environment.GetEnvironmentVariable("FrontendUrl")?.TrimEnd('/');

        string confirmLink = frontendUrl + "/confirm-email?token=" + UrlSafeConverter.ToUrlSafe(token) + "&email=" +
                             UrlSafeConverter.ToUrlSafe(_encryptor.Encrypt(email));
        MimeMessage message = MimeMessages.GetConfirmEmailMessage(email, confirmLink);
        try
        {
            _logger.LogInformation("Attempting to send confirmation email to: {Email}", email);
            await _audit.LogUserActivityAsync(user.Id, "Sent confirmation E-mail",
                "User has requested to confirm their E-mail address.", LogType.Email);
            await _client.SendAsync(message);
            _logger.LogInformation("Confirmation email sent successfully to: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to: {Email}", email);
            return LogicResult<bool>.Error($"Failed to send confirmation email: {ex.Message}");
        }

        _logger.LogInformation("Successfully sent ConfirmEmail to {email}.", email);
        return LogicResult<bool>.Ok(true);
    }


    public async Task<LogicResult<bool>> ConfirmEmailAsync(string token, string email)
    {
        var decodedToken = UrlSafeConverter.FromUrlSafe(token);
        var decodedEmail = _encryptor.Decrypt(UrlSafeConverter.FromUrlSafe(email));
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(decodedToken) ||
            string.IsNullOrEmpty(decodedEmail))
        {
            _logger.LogWarning("Token is required.");
            return LogicResult<bool>.BadRequest("Token is required.");
        }

        AppUser? user = await _userUtils.GetAndUpgradeUserByEmailAsync(decodedEmail);
        if (user is null)
        {
            _logger.LogWarning("User not found.");
            return LogicResult<bool>.NotFound("User not found.");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogWarning("E-mail already confirmed for User: {Email}", user.Email);
            return LogicResult<bool>.Conflict("E-mail already confirmed.");
        }

        _logger.LogInformation("Attempting to confirm email for User: {Email}", user.Email);
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (result.Succeeded)
        {
            _logger.LogInformation("Email confirmed for User: {Email}", user.Email);
            await _audit.LogUserActivityAsync(user.Id, "E-mail has been confirmed",
                "User has requested to confirm their E-mail address.", LogType.Email);

            await _userManager.UpdateSecurityStampAsync(user);
            return LogicResult<bool>.Ok(true);
        }

        _logger.LogError("Failed to confirm email for User: {Email}", user.Email);
        return LogicResult<bool>.Error("Failed to confirm email. Please try again later.");
    }

    public async Task<LogicResult<bool>> SendForgotPasswordEmailAsync(string email, bool? isAccountRecovery = false)
    {
        _logger.LogInformation("Starting SendResetEmailAsync.");
        if (!Regex.IsMatch(email, Regexes.Email))
        {
            return LogicResult<bool>.BadRequest("Invalid email format.");
        }

        Expression<Func<AppUser, bool>>? predicate = isAccountRecovery == true ? u => u.Flagged : null;
        var user = await _userUtils.GetAndUpgradeUserByEmailAsync(email, isAccountRecovery == true, predicate);
        if (user is null) return LogicResult<bool>.Ok(true);

        string safeForgetToken = UrlSafeConverter.ToUrlSafe(await _userManager.GeneratePasswordResetTokenAsync(user));
        string safeEncryptedUserId = UrlSafeConverter.ToUrlSafe(_encryptor.Encrypt(user.Id.ToString()));

        var frontendUrl = Environment.GetEnvironmentVariable("FrontendUrl")?.TrimEnd('/');
        string resetLink = $"{frontendUrl}/reset-password?userId={safeEncryptedUserId}&token={safeForgetToken}";

        var message = isAccountRecovery == true
            ? MimeMessages.GetReactivateAccountEmail(email, resetLink)
            : MimeMessages.GetForgotPassword(email, resetLink);

        try
        {
            _logger.LogInformation("Attempting to send password reset email to: {Email}", user.Email);
            await _client.SendAsync(message);
            await _audit.LogUserActivityAsync(user.Id, "Requested password recovery",
                "User has requested to recover their password.", LogType.Password);
            _logger.LogInformation("Password reset email sent successfully to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to: {Email}", user.Email);
            return LogicResult<bool>.Error($"Failed to send password reset email: {ex.Message}");
        }

        _logger.LogInformation(nameof(SendForgotPasswordEmailAsync) + " completed successfully.");
        return LogicResult<bool>.Ok(true);
    }

    public async Task<LogicResult<bool>> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        if (dto == null) return LogicResult<bool>.BadRequest("Invalid reset data");

        if (dto.token == null) return LogicResult<bool>.BadRequest("Invalid token");
        string escapedToken = UrlSafeConverter.FromUrlSafe(dto.token);
        if (dto.password == null) return LogicResult<bool>.BadRequest("Invalid password");
        if (dto.encryptedUserId == null) return LogicResult<bool>.BadRequest("Invalid email");
        string escapedUserId = UrlSafeConverter.FromUrlSafe(dto.encryptedUserId);

        string decryptedUserId = _encryptor.Decrypt(escapedUserId);
        if (decryptedUserId == null) return LogicResult<bool>.BadRequest("Invalid UserId");
        var user = await _userManager.FindByIdAsync(decryptedUserId);
        if (user == null) return LogicResult<bool>.NotFound("No such User.");
        _logger.LogInformation("Match: {name}", user.UserName);
        var res = await _userManager.ResetPasswordAsync(user, escapedToken, dto.password);
        _logger.LogInformation(res.ToString());

        if (res.Succeeded)
        {
            _logger.LogInformation("Password reset successful for User: {Name}", user.UserName);
            await _audit.LogUserActivityAsync(user.Id, "Password has been reset", "User has recovered their password.",
                LogType.Password);

            return LogicResult<bool>.Ok(true);
        }

        _logger.LogError("Password reset failed for User: {Name}", user.UserName);
        return LogicResult<bool>.Error("Failed to reset password. Please try again later.");
    }

    public async Task<LogicResult<bool>> SendEmailChangeAsync(ChangeEmailDto dto, string? userId)
    {
        var (previousEmail, newEmail) = dto;
        if (!Regex.IsMatch(previousEmail, Regexes.Email) || !Regex.IsMatch(newEmail, Regexes.Email))
        {
            return LogicResult<bool>.BadRequest("E-mail format is invalid.");
        }

        var isNewEmailAvailable = await _userUtils.CheckEmailAvailabilityAsync(newEmail);
        if (!isNewEmailAvailable) return LogicResult<bool>.Conflict("This E-mail is already in use.");
        AppUser? user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return LogicResult<bool>.NotFound("Failed to find User");
        }

        bool emailBelongsToUser = user.NormalizedEmail.Equals(_userManager.NormalizeEmail(previousEmail));
        if (!emailBelongsToUser)
        {
            return LogicResult<bool>.BadRequest("Please enter your actual e-mail");
        }

        string changeToken = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        string safeChangeToken = UrlSafeConverter.ToUrlSafe(changeToken);

        string safeOldEmail = UrlSafeConverter.ToUrlSafe(_encryptor.Encrypt(previousEmail));
        string safeNewEmail = UrlSafeConverter.ToUrlSafe(_encryptor.Encrypt(newEmail));

        string changeUrl = apiUrl + "/email/change-email?token=" + safeChangeToken + "&oldEmail=" + safeOldEmail +
                           "&newEmail=" + safeNewEmail;
        var message = MimeMessages.GetEmailChange(previousEmail, newEmail, changeUrl);
        await _client.SendSecurityUpdateAsync(previousEmail,
            "Your account has been requested to be transferred to another E-mail address.",
            SecurityAlertSeverity.WARNING);
        try
        {
            await _audit.LogUserActivityAsync(user.Id, "Requested E-mail change",
                "User has requested to change their E-mail address.", LogType.Email);

            await _client.SendAsync(message);
            return LogicResult<bool>.Ok(true);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return LogicResult<bool>.Error("Something went wrong. Please try again later");
    }

    public async Task<LogicResult<SelfResponse>> ConfirmEmailChangeAsync(ConfirmEmailChangeDto dto, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return LogicResult<SelfResponse>.Unauthenticated("Please log in first");
        }

        var (newEmail, oldEmail, token) = dto;
        if (string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(oldEmail) || string.IsNullOrEmpty(token))
        {
            return LogicResult<SelfResponse>.BadRequest("Missing attributes");
        }

        string decryptedOldEmail = _encryptor.Decrypt(oldEmail);
        string decryptedNewEmail = _encryptor.Decrypt(newEmail);

        if (!Regex.IsMatch(decryptedOldEmail, Regexes.Email))
        {
            return LogicResult<SelfResponse>.BadRequest("Current email is invalid.");
        }

        if (!Regex.IsMatch(decryptedNewEmail, Regexes.Email))
        {
            return LogicResult<SelfResponse>.BadRequest("New email is invalid.");
        }

        AppUser? user = await _userUtils.GetAndUpgradeUserByEmailAsync(decryptedOldEmail);
        string notFoundMessage = "Couldn't find User by this e-mail";
        if (user == null)
        {
            return LogicResult<SelfResponse>.NotFound(notFoundMessage);
        }

        if (!user.Id.ToString().Equals(userId))
        {
            return LogicResult<SelfResponse>.Unauthorized(notFoundMessage);
        }

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            IdentityResult res =
                await _userManager.ChangeEmailAsync(user, decryptedNewEmail, UrlSafeConverter.FromUrlSafe(token));
            if (!res.Succeeded)
            {
                return LogicResult<SelfResponse>.Error(
                    "Update failed. Please keep using your original E-mail, and try again later.");
            }

            user.EmailConfirmed = false;
            user.PlaintextEmailForIndexing = decryptedNewEmail;
            await _audit.LogUserActivityAsync(user.Id, "E-mail address changed", "User has changed their E-mail address.",
                LogType.Email);
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return LogicResult<SelfResponse>.Ok(new(user, await _perms.GetFullPermissionsAsync(user.Id)));
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return LogicResult<SelfResponse>.Error(
                "Failed to update your e-mail address. Please keep using your original E-mail, and try again later.");
        }
    }
}