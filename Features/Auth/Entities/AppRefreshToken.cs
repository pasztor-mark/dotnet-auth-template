using System.ComponentModel.DataAnnotations;
using auth_template.Entities.Data;
using auth_template.Features.Auth.Configuration;
using Microsoft.EntityFrameworkCore;

namespace auth_template.Features.Auth.Entities;

[Index(nameof(UserAgentIndex))] 
[Index(nameof(IpAddressIndex))]  
[Index(nameof(TokenString))]
public class AppRefreshToken
{
    public AppRefreshToken()
    {
    }

    public AppRefreshToken(string tokenString, Guid userId)
    {
        ExpiryUtc = DateTime.UtcNow.AddMonths(AuthConfiguration.RefreshTokenExpirationInMonths);
        TokenString = tokenString;
        AppUserId = userId;
    }
    public AppRefreshToken(string tokenString, Guid userId, string? userAgent, string? userAgentIndex, int indexVersion)
    {
        ExpiryUtc = DateTime.UtcNow.AddMonths(AuthConfiguration.RefreshTokenExpirationInMonths);
        TokenString = tokenString;
        AppUserId = userId;
        UserAgent = userAgent;
        UserAgentIndex = userAgentIndex;
        IndexVersion = indexVersion;
    }
    public AppRefreshToken(string tokenString, Guid userId, string? userAgent, string? ipAddress, string? userAgentIndex, string? ipAddressIndex, int indexVersion)
    {
        ExpiryUtc = DateTime.UtcNow.AddMonths(AuthConfiguration.RefreshTokenExpirationInMonths);
        TokenString = tokenString;
        AppUserId = userId;
        UserAgent = userAgent;
        UserAgentIndex = userAgentIndex;
        IpAddress = ipAddress;
        IpAddressIndex = ipAddressIndex;
        IndexVersion = indexVersion;
    }
    [Required]
    public Guid Id { get; set; }
    public string TokenString { get; set; }
    public DateTime ExpiryUtc { get; set; }
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string UserAgent { get; set; }
    [Required]
    public string UserAgentIndex { get; set; }
    [MaxLength(255)]
    public string? IpAddress { get; set; }
    [Required]
    public string? IpAddressIndex { get; set; }
    
    
    public int IndexVersion { get; set; }
}