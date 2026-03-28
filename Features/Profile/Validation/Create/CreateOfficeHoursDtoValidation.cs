using auth_template.Features.Profile.Transfer.Create;
using FluentValidation;

namespace auth_template.Features.Profile.Validation.Create;

public class CreateOfficeHoursDtoValidation : AbstractValidator<CreateOfficeHoursDto>
{
    public CreateOfficeHoursDtoValidation()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Please select a valid day of the week.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required.");

        RuleFor(x => x)
            .Must(x => x.StartTime < x.EndTime)
            .WithMessage("Start time must be earlier than end time.");

        RuleFor(x => x)
            .Must(x => (x.EndTime.ToTimeSpan() - x.StartTime.ToTimeSpan()).TotalMinutes >= 15)
            .WithMessage("An office hour slot must be at least 15 minutes long.");
            
        RuleFor(x => x)
            .Must(x => (x.EndTime.ToTimeSpan() - x.StartTime.ToTimeSpan()).TotalHours <= 12)
            .WithMessage("An office hour slot cannot exceed 12 hours.");
    }
}