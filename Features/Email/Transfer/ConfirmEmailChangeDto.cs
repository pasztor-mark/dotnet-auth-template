namespace auth_template.Features.Email.Transfer;

public class ConfirmEmailChangeDto(string newEmail, string oldEmail, string token)
{
    public string newEmail { get; init; } = newEmail;
    public string oldEmail { get; init; } = oldEmail;
    public string token { get; init; } = token;

    
    public void Deconstruct(out string newEmail, out string oldEmail, out string token)
    {
        newEmail = this.newEmail;
        oldEmail = this.oldEmail;
        token = this.token;
    }
}
