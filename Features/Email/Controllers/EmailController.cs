using System.Security.Claims;
using auth_template.Enums;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Email.Services;
using auth_template.Features.Email.Transfer;
using auth_template.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace auth_template.Features.Email.Controllers;

[ApiController]
[Route("api/email")]
[Authorize]
public class EmailController(IEmailService _email, IHttpContextAccessor _http) : ControllerBase
{
    // POST to send confirm E-mail
    [HttpPost("confirm/send")]
    [EnableRateLimiting(nameof(RateLimits.Email))]
    public async Task<ActionResult<bool>> GetConfirmationEmailAsync()
    {
        return ResponseUtility<bool>.HttpResponse(await _email.GetConfirmationEmailAsync());
    }
    
    // POST to confirm E-mail
    [HttpPost("confirm")]
    [AllowAnonymous]
    [EnableRateLimiting(nameof(RateLimits.Login))]
    public async Task<ActionResult<bool>> ConfirmEmailAsync([FromQuery] ConfirmEmailDto dto)
    {
        if (string.IsNullOrEmpty(dto.token) || string.IsNullOrEmpty(dto.email))
        {
            return BadRequest("Token and email are required.");
        }
        return ResponseUtility<bool>.HttpResponse(await _email.ConfirmEmailAsync(dto.token, dto.email));
    }
    
    // POST to get password reset E-mail
    [HttpPost("forgot-password/send")]
    [AllowAnonymous]    
    [EnableRateLimiting(nameof(RateLimits.PasswordChange))]
    public async Task<ActionResult<bool>> SendRecoveryEmailAsync([FromBody] SendEmailDto dto)
    {
        return ResponseUtility<bool>.HttpResponse(await _email.SendForgotPasswordEmailAsync(dto.email));
    }
    
    // POST to reset password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(nameof(RateLimits.Login))]
    public async Task<ActionResult<bool>> ResetPasswordAsync([FromBody] ForgotPasswordDto dto)
    {
        return ResponseUtility<bool>.HttpResponse(await _email.ForgotPasswordAsync(dto));
    }
    
    // POST to get E-mail change email
    [HttpPost("change-email/send")]
    [EnableRateLimiting(nameof(RateLimits.Email))]
    public async Task<ActionResult<bool>> SendEmailChangeAsync([FromBody] ChangeEmailDto dto)
    {
        string? userId = GetUserId();
        return ResponseUtility<bool>.HttpResponse(await _email.SendEmailChangeAsync(dto, userId));
    }
    
    // POST to change E-mail
    [HttpPost("change-email")]
    [EnableRateLimiting(nameof(RateLimits.Email))]
    public async Task<ActionResult<SelfResponse>> ConfirmEmailChangeAsync([FromBody] ConfirmEmailChangeDto dto)
    {
        string? userId = GetUserId();
        return ResponseUtility<SelfResponse>.HttpResponse(await _email.ConfirmEmailChangeAsync(dto, userId));
    }

    private string? GetUserId()
    {
        return _http.HttpContext?.User.FindFirst("sub")?.Value 
               ?? _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}