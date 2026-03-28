using auth_template.Features.Profile.Enums;
using auth_template.Features.Profile.Transfer.Create;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Create;

public class CreateContactLinkDtoValidation : AbstractValidator<CreateContactLinkDto>
{
    public CreateContactLinkDtoValidation()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Please select a supported link type.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL cannot be empty.")
            .Must(BeAValidUrl).WithMessage("Please enter a valid HTTP/HTTPS URL.");

        RuleFor(x => x.Url)
            .Must((dto, x) => ValidationRules.ValidateDomainMatch(dto.Type, x))
            .WithMessage(x => $"The URL provided does not look like a valid {Enum.GetName(typeof(LinkType), x.Type)} link.");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}