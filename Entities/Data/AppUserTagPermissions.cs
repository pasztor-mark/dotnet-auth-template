namespace auth_template.Entities.Data;

public class AppUserTagPermissions
{
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }
    public AppUserTag Tag { get; set; }
}