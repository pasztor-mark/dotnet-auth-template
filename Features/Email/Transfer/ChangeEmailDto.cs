namespace auth_template.Features.Email.Transfer;

public class ChangeEmailDto
{
    public void Deconstruct(out string previousEmail, out string nextEmail)
    {
        previousEmail = PreviousEmail;
        nextEmail = NextEmail;
    }

    public string PreviousEmail { get; set; }
    public string NextEmail { get; set; }

    public ChangeEmailDto()
    {
        
    }
    
    
}