namespace auth_template.Features.Email.Transfer;

public class ConfirmEmailDto
{
    public ConfirmEmailDto() { }

    public ConfirmEmailDto(string token, string email)
    {
        this.token = token;
        this.email = email;
    }
    public void Deconstruct(out string token, out string email)
    {
        token = this.token;
        email = this.email;
    }

    public string token { get; set; }
    public string email { get; set; }
}
