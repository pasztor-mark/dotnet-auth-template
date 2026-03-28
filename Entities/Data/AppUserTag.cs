using auth_template.Entities.Interfaces;

namespace auth_template.Entities.Data;

public class AppUserTag : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ICollection<AppTagPermission> TagPermissions { get; set; }
    public ICollection<AppUserTagAssignment> TagAssignments  { get; set; }
    public bool Flagged { get; set; }
    public DateTime? DeletedAt { get; set; }
    
}