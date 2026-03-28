namespace auth_template.Features.Profile.Transfer.Update;

public record ReplaceOfficeHoursDto(
    DayOfWeek? DayOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime
);