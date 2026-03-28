namespace auth_template.Features.Profile.Transfer.Create;

public record CreateContactDto(
    string ContactDescription,
    string? PhoneNumber,
    string? EmailAddress
)
{
    public void Deconstruct(out string contactDescription, out string? phoneNumber, out string? emailAddress)
    {
        contactDescription = ContactDescription;
        phoneNumber = PhoneNumber;
        emailAddress = EmailAddress;
    }
}