using auth_template.Features.Profile.Enums;

namespace auth_template.Features.Profile.Transfer.Update;

public record ReplaceContactLinkDto(
    LinkType? Type,
    string? Url
);