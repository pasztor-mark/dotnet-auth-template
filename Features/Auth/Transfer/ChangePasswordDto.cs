namespace auth_template.Features.Auth.Transfer;

public class ChangePasswordDto(string newPassword, string currentPassword)
{
    public string newPassword { get; init; } = newPassword;
    public string currentPassword { get; init; } = currentPassword;

    public void Deconstruct(out string newPassword, out string currentPassword)
    {
        newPassword = this.newPassword;
        currentPassword = this.currentPassword;
    }
}