using auth_template.Entities.Data;

namespace auth_template.Utilities.Security.Passwords;

public interface IPasswordVerifier
{
    Task<bool> VerifyPasswordAsync(AppUser user, string plainPassword);
}