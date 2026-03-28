namespace auth_template.Features.Profile.Transfer.Create;

public record CreateOfficeHoursDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);