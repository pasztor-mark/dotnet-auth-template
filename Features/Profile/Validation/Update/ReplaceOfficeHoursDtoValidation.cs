using auth_template.Features.Profile.Transfer.Update;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Update;

public class ReplaceOfficeHoursDtoValidation : AbstractValidator<ReplaceOfficeHoursDto>
{
    public ReplaceOfficeHoursDtoValidation()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Please select a valid day of the week.")
            .When(x => x.DayOfWeek.HasValue);

        RuleFor(x => x)
            .Must(x => x.StartTime < x.EndTime)
            .WithMessage("The start time must be earlier than the end time.")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
    }
}