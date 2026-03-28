namespace auth_template.Features.Auth.Responses.Permissions;

public record PermissionTransfer
{
    public List<string> Tags { get; set; } = [];
    public List<string> Features { get; set; } = [];
}