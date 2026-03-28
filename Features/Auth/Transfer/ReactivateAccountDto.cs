using System.ComponentModel.DataAnnotations;

namespace auth_template.Features.Auth.Transfer;

public class ReactivateAccountDto
{
    [EmailAddress]
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string Username { get; set; }

    public void Deconstruct(out string email, out string displayName, out string username)
    {
        email = Email;
        displayName = DisplayName;
        username = Username;
    }
}