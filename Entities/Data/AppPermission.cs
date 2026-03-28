namespace auth_template.Entities.Data;

public class AppPermission
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<AppTagPermission> TagPermissions { get; set; }
    
    public bool Flagged { get; set; }
    public DateTime? DeletedAt { get; set; }
}