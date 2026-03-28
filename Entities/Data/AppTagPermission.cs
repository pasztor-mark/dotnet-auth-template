using auth_template.Entities.Interfaces;

namespace auth_template.Entities.Data;

public class AppTagPermission : ISoftDeletable
{
    public Guid TagId { get; set; }
    public AppUserTag Tag { get; set; }

    public Guid PermissionId { get; set; }
    public AppPermission Permission { get; set; }

    public bool Flagged { get; set; }
    public DateTime? DeletedAt { get; set; }
}