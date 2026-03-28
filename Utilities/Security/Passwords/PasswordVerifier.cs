using auth_template.Entities.Data;
using Microsoft.AspNetCore.Identity;

namespace auth_template.Utilities.Security.Passwords;
public class PasswordVerifier(UserManager<AppUser> _user) : IPasswordVerifier
{
    public async Task<bool> VerifyPasswordAsync(AppUser user, string plainPassword)
    {
        return await _user.CheckPasswordAsync(user, plainPassword);
    }
}