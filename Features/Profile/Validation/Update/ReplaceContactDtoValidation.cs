using auth_template.Configuration;
using auth_template.Features.Profile.Transfer.Update;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Update;

public class ReplaceContactDtoValidator : AbstractValidator<ReplaceContactDto>
{
    public ReplaceContactDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.phoneNumber) || !string.IsNullOrWhiteSpace(x.emailAddress) || !string.IsNullOrWhiteSpace(x.contactDescription))
            .WithMessage("At least one contact method (Phone or Email) or description must be provided to replace the current entry.");

        RuleFor(x => x.phoneNumber)
            .Matches(Regexes.PhoneNumber)
            .WithMessage("Invalid phone number format. Use international format (e.g., +36301234567).")
            .When(x => !string.IsNullOrWhiteSpace(x.phoneNumber));

        RuleFor(x => x.emailAddress)
            .EmailAddress().WithMessage("A valid email address is required.")
            .When(x => !string.IsNullOrWhiteSpace(x.emailAddress));
        RuleFor(x => x.contactDescription)
            .MaximumLength(50).WithMessage("Description cannot exceed 50 characters.");

    }
}