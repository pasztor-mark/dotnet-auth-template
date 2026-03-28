using auth_template.Features.Profile.Transfer.Update;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Update;

public class UpdateProfilePictureDtoValidator : AbstractValidator<UpdateProfilePictureDto>
{
    public UpdateProfilePictureDtoValidator()
    {
        RuleFor(x => x.url)
            .NotEmpty().WithMessage("Avatar URL is required.")
            .Must(BeAValidImageUrl).WithMessage("The URL must point to a valid image (jpg, jpeg, png, webp, gif).")
            .Must(BeSecureUrl).WithMessage("Only secure (HTTPS) URLs are allowed.");
            
    }

    private bool BeAValidImageUrl(string url)
    {
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp"};
        return validExtensions.Any(ext => url.ToLower().Split('?')[0].EndsWith(ext));
    }

    private bool BeSecureUrl(string url)
    {
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}