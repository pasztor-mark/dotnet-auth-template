using auth_template.Features.Profile.Configuration;
using auth_template.Features.Profile.Transfer.Update;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Update;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateCoreProfileDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(ProfileConfiguration.MAX_DISPLAY_NAME_LENGTH).WithMessage("Display name is too long")
            .MinimumLength(ProfileConfiguration.MIN_DISPLAY_NAME_LENGTH).When(x => x.DisplayName != null);

        RuleFor(x => x.Bio)
            .MaximumLength(ProfileConfiguration.MAX_BIO_LENGTH).WithMessage($"Bio cannot exceed {ProfileConfiguration.MAX_BIO_LENGTH} characters");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow).WithMessage("Birth date cannot be in the future");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.DisplayName) || 
                       !string.IsNullOrWhiteSpace(x.Bio) || 
                       !string.IsNullOrWhiteSpace(x.Headline) || 
                       !string.IsNullOrWhiteSpace(x.Location) || 
                       x.DateOfBirth.HasValue ||
                       x.IsPublic.HasValue)
            .WithMessage("At least one field must be provided for update.");
    }
}