using auth_template.Configuration;
using auth_template.Features.Auth.Configuration;
using auth_template.Features.Auth.Transfer;
using auth_template.Validation;
using FluentValidation;

namespace auth_template.Features.Auth.Validation;

public class RegisterDtoValidation : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidation()
    {
        RuleFor(x => x.email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(Regexes.Email)
            .WithMessage("Invalid email format.")
            .MaximumLength(AuthConfiguration.MaximumEmailLength)
            .WithMessage($"Email must be shorter than {AuthConfiguration.MaximumEmailLength}.");

        RuleFor(x => x.password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(AuthConfiguration.MinimumPasswordLength).WithMessage(
                $"Password must be at least {AuthConfiguration.MinimumPasswordLength} characters long.")
            .MaximumLength(AuthConfiguration.MaximumPasswordLength)
            .WithMessage($"Password must be at most {AuthConfiguration.MaximumPasswordLength} long.")
            .Matches(Regexes.SpecialCharacters).WithMessage("Password must contain a special character")
            .Matches(Regexes.Uppercase).WithMessage("Password must contain an uppercase letter")
            .Matches(Regexes.Lowercase).WithMessage("Password must contain a lowercase letter")
            .Matches(Regexes.Digits).WithMessage("Password must contain a number");
        RuleFor(x => x.username)
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