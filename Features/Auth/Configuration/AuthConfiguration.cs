namespace auth_template.Features.Auth.Configuration;

public class AuthConfiguration
{
    public const int MaximumEmailLength = 96;

    public const int MinimumPasswordLength = 6;
    public const int MaximumPasswordLength = 255;
    public const int MinimumUsernameLength = 3;
    public const int MaximumUsernameLength = 32;

    public const int JwtExpirationInSeconds = 600; // 10 minutes
    public const int RefreshTokenExpirationInMonths = 3; // 3 months
    public const int AccessFailCountThreshold = 5; // Number of failed access attempts before locking the account
    public const int AccessFailLockoutDurationInMinutes = 5; // Duration of lock

    public const int
        RefreshTokenReplacementGracePeriodInDays =
            7; // If a refresh token is used within the grace period, a new one will be automatically issued.

    public const int PasswordChangeCooldownInDays = 7;
    public static TimeSpan RemoveAfterFlagging = TimeSpan.FromDays(30);
}