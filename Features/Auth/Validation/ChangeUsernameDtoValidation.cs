using auth_template.Configuration;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Transfer;
using FluentValidation;

namespace auth_template.Features.Auth.Validation;

public class ChangeUsernameDtoValidation : AbstractValidator<ChangeUsernameDto>
{
    public ChangeUsernameDtoValidation()
    {
        RuleFor(x => x.newUsername)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(AuthConfiguration.MinimumUsernameLength).WithMessage(
                $"Username must be at least {AuthConfiguration.MinimumUsernameLength} characters long.")
            .MaximumLength(AuthConfiguration.MaximumUsernameLength)
            .WithMessage($"Username must be at most {AuthConfiguration.MaximumUsernameLength} long.")
            .Matches(Regexes.FirstLetter).WithMessage("Username must start with a letter")
            .Matches(Regexes.AllowedUsernameCharacters)
            .WithMessage("Username can only contain letters, numbers, dots, and underscores");
    }
}