using auth_template.Features.Profile.Enums;

namespace auth_template.Features.Profile.Transfer.Create;

public class CreateContactLinkDto(
    LinkType Type,
    string Url
)

{
    public LinkType Type { get; init; } = Type;
    public string Url { get; init; } = Url;

    public void Deconstruct(out LinkType type, out string url)
    {
        type = Type;
        url = Url;
    }
}
