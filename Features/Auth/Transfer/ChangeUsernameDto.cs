namespace auth_template.Features.Auth.Transfer;

public class ChangeUsernameDto(string newUsername)
{
    public string newUsername { get; init; } = newUsername;

    public void Deconstruct(out string newUsername)
    {
        newUsername = this.newUsername;
    }
}