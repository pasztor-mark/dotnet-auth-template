namespace auth_template.Features.Profile.Transfer.Update;

public record UpdateCoreProfileDto(
    string? DisplayName,
    string? Bio,
    string? Headline,
    string? Location,
    DateTime? DateOfBirth,
    bool? IsPublic
);