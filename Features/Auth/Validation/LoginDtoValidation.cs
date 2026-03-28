using auth_template.Configuration;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Transfer;
using auth_template.Validation;
using FluentValidation;

namespace auth_template.Features.Auth.Validation;

/// <summary>
///     Validator for LoginDTO. Ensures either email or username is provided, and password meets required criteria.
/// </summary>
public class LoginDTOValidation : AbstractValidator<LoginDto>
{
    public LoginDTOValidation()
    {
        RuleFor(x => x)
            .Must(dto => !string.IsNullOrWhiteSpace(dto.email) || !string.IsNullOrWhiteSpace(dto.username))
            .WithMessage("Either email or username is required.");

        RuleFor(x => x.email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.email)).WithMessage("Invalid email format.")
            .MaximumLength(AuthConfiguration.MaximumEmailLength).When(x => !string.IsNullOrWhiteSpace(x.email))
            .WithMessage($"Email must be shorter than {AuthConfiguration.MaximumEmailLength}.");
            

        RuleFor(x => x.password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(AuthConfiguration.MinimumPasswordLength)
            .WithMessage($"Password must be at least {AuthConfiguration.MinimumPasswordLength} characters long.")
            .MaximumLength(AuthConfiguration.MaximumPasswordLength)
            .WithMessage($"Password must be at most {AuthConfiguration.MaximumPasswordLength} long.")
            .Matches(Regexes.PasswordSpecialCharacters).WithMessage("Password must contain a special character")
            .Matches(Regexes.Uppercase).WithMessage("Password must contain an uppercase letter")
            .Matches(Regexes.Lowercase).WithMessage("Password must contain a lowercase letter")
            .Matches(Regexes.Digits).WithMessage("Password must contain a number");

        RuleFor(x => x.username)
            .MaximumLength(AuthConfiguration.MaximumUsernameLength).When(x => !string.IsNullOrWhiteSpace(x.username))
            .WithMessage($"Username must be at most {AuthConfiguration.MaximumUsernameLength} long.")
            .MinimumLength(AuthConfiguration.MinimumUsernameLength)
            .WithMessage($"Username must be at least {AuthConfiguration.MinimumUsernameLength} characters long.")
            .Matches(Regexes.FirstLetter).WithMessage("Username must start with a letter")
            .Matches(Regexes.AllowedUsernameCharacters)
            .WithMessage("Username can only contain letters, numbers, dots, and underscores");
    }
}