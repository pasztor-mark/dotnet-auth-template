using auth_template.Configuration;
using auth_template.Features.Profile.Transfer.Create;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Create;

public class CreateContactDtoValidation : AbstractValidator<CreateContactDto>
{
    public CreateContactDtoValidation()
    {
        RuleFor(x => x.ContactDescription)
            .NotEmpty().WithMessage("Description is mandatory (e.g., 'Primary', 'Office').")
            .MaximumLength(50).WithMessage("Description cannot exceed 50 characters.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PhoneNumber) || !string.IsNullOrWhiteSpace(x.EmailAddress))
            .WithMessage("At least one contact method (Phone or Email) must be provided.");

        RuleFor(x => x.PhoneNumber)
            .Matches(Regexes.PhoneNumber)
            .WithMessage("Invalid phone number format. Use international format.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.EmailAddress)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress));
    }
}