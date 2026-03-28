using System.Security.Claims;
using auth_template.Entities;
using auth_template.Enums;
using auth_template.Features.Auth.Responses;
using auth_template.Features.Auth.Services;
using auth_template.Features.Auth.Transfer;
using auth_template.Options;
using auth_template.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService _authService, IHttpContextAccessor _http, AppDbContext _ctx) : ControllerBase
{
    //POST to register new user
    [HttpPost("register")]
    [EnableRateLimiting(nameof(RateLimits.Register))]
    public async Task<ActionResult<SelfResponse>> RegisterUserAsync([FromBody] RegisterDto registerDto)
    {
        string? userAgent = _http.HttpContext.Request.Headers.UserAgent;
        string? ipAddress = _http.HttpContext.Connection.RemoteIpAddress.ToString();
        return ResponseUtility<SelfResponse>.HttpResponse(
            await _authService.RegisterUserAsync(registerDto, userAgent, ipAddress));
    }

    //POST a login request
    [HttpPost("login")]
    [EnableRateLimiting(nameof(RateLimits.Login))]
    public async Task<ActionResult<SelfResponse>> LoginAsync([FromBody] LoginDto loginDto)
    {
        string? userAgent = _http.HttpContext.Request.Headers.UserAgent;
        string? ipAddress = _http.HttpContext.Connection.RemoteIpAddress.ToString();

        return ResponseUtility<SelfResponse>.HttpResponse(
            await _authService.LoginUserAsync(loginDto, userAgent, ipAddress));
    }

    //POST a token refresh request
    [HttpPost("refresh")]
    [EnableRateLimiting(nameof(RateLimits.Refresh))]
    public async Task<ActionResult<bool>> RefreshAsync()
    {
        return ResponseUtility<bool>.HttpResponse(await _authService.RefreshAsync());
    }

    //POST to log out from one device
    [Route("logout/device")]
    [EnableRateLimiting(nameof(RateLimits.General))]
    [Authorize]
    public async Task<ActionResult<bool>> LogoutFromDeviceAsync()
    {
        return ResponseUtility<bool>.HttpResponse(await _authService.LogoutFromDeviceAsync());
    }

    //POST to log out from all devices
    [Route("logout")]
    [Authorize]
    public async Task<ActionResult<bool>> LogoutFromAllDevicesAsync()
    {
        return ResponseUtility<bool>.HttpResponse(await _authService.LogoutFromAllDevicesAsync());
    }

    [EnableRateLimiting(nameof(RateLimits.Heartbeat))]
    [HttpPost("heartbeat")]
    [Authorize]
    public void PostHeartbeat([FromBody] ActivityDto dto)
    {
        string userId = _http.HttpContext?.User.FindFirst("sub")?.Value ??
                        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _authService.PostHeartbeat(userId, dto);
    }

    //---

    //GET self
    [HttpGet("self")]
    [Authorize]
    public async Task<ActionResult<SelfResponse>> GetSelfAsync()
    {
        string userId = _http.HttpContext?.User.FindFirst("sub")?.Value ??
                        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var res = await _authService.GetSelfAsync(userId);
        return ResponseUtility<SelfResponse>.HttpResponse(res);
    }

    //GET to check email availability
    [HttpGet("availability/email")]
    [EnableRateLimiting(nameof(RateLimits.Search))]
    public async Task<ActionResult<bool>> CheckEmailAvailabilityAsync(string? emailAddress)
    {
        return ResponseUtility<bool>.HttpResponse(await _authService.CheckEmailAvailabilityAsync(emailAddress));
    }

    //GET to check user availability
    [HttpGet("availability/username")]
    [EnableRateLimiting(nameof(RateLimits.Search))]
    public async Task<ActionResult<bool>> CheckUsernameAvailabilityAsync(string? userName)
    {
        return ResponseUtility<bool>.HttpResponse(await _authService.CheckUsernameAvailabilityAsync(userName));
    }

    //GET user preferences
    [Authorize]
    [HttpGet("preferences")]
    [EnableRateLimiting(nameof(RateLimits.General))]
    public async Task<ActionResult<PreferenceResponse>> GetPreferencesAsync()
    {
        string userId = _http.HttpContext?.User.FindFirst("sub")?.Value ??
                        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ResponseUtility<PreferenceResponse>.HttpResponse(await _authService.GetPreferencesAsync(userId));
    }

    //GET user preferences
    [HttpGet("search/tag")]
    [EnableRateLimiting(nameof(RateLimits.Search))]
    public async Task<ActionResult<List<TagListingResponse>>> SearchTagsAsync([FromQuery] string searchTerm)
    {
        return ResponseUtility<List<TagListingResponse>>.HttpResponse(await _authService.SearchTagsAsync(searchTerm));
    }

    //---

    //PATCH to change password
    [Authorize]
    [EnableRateLimiting(nameof(RateLimits.PasswordChange))]
    [HttpPatch("change/password")]
    public async Task<ActionResult<bool>> ChangePasswordAsync([FromBody] ChangePasswordDto dto)
    {
        string userId = _http.HttpContext?.User.FindFirst("sub")?.Value ??
                        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ResponseUtility<bool>.HttpResponse(await _authService.ChangePasswordAsync(dto, userId));
    }

    //PATCH to change username
    [Authorize]
    [EnableRateLimiting(nameof(RateLimits.UserUpdate))]
    [HttpPatch("change/username")]
    public async Task<ActionResult<bool>> ChangeUsernameAsync([FromBody] ChangeUsernameDto dto)
    {
        string userId = _http.HttpContext?.User.FindFirst("sub")?.Value ??
                        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ResponseUtility<bool>.HttpResponse(await _authService.ChangeUsernameAsync(dto, userId));
    }

    //PATCH to update preferences

    //---

    //PUT to recover deactivated account
    [HttpPut("me/reactivate")]
    public async Task<ActionResult<bool>> ReactivateAccountAsync(ReactivateAccountDto dto)
    {
        return ResponseUtility<bool>.HttpResponse(await _authService.RecoverFlaggedUserAsync(dto));
    }

    //DELETE to flag account for deactivation
    [Authorize]
    [HttpDelete("me/delete")]
    public async Task<ActionResult<bool>> FlagUserForAnonymizationAsync()
    {
        string userId = _http.HttpContext?.User.FindFirst("sub")?.Value ??
                        _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ResponseUtility<bool>.HttpResponse(await _authService.FlagUserForAnonymizationAsync(userId));
    }

    [HttpGet("good-morning")]
    public async Task<ActionResult> WakeUpAsync(CancellationToken ct)
    {
        await _ctx.Database.ExecuteSqlRawAsync("SELECT 1", ct);
        return Ok();
    }
    
    [HttpPost("register/admin")]
    [EnableRateLimiting(nameof(RateLimits.Register))]
    public async Task<ActionResult<SelfResponse>> RegisterAdminAsync(
        [FromBody] RegisterDto registerDto, 
        [FromServices] Microsoft.Extensions.Options.IOptions<SecurityOptions> securityOptions)
    {
        if (registerDto.password != securityOptions.Value.AdminPassword)
        {
            return Unauthorized();
        }

        string? userAgent = _http.HttpContext.Request.Headers.UserAgent;
        string? ipAddress = _http.HttpContext.Connection.RemoteIpAddress?.ToString();

        return ResponseUtility<SelfResponse>.HttpResponse(
            await _authService.RegisterAdminAsync(registerDto, userAgent, ipAddress));
    }
}