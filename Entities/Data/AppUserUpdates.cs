using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using auth_template.Features.Auth.Enums;

namespace auth_template.Entities.Data;

public class AppUserUpdates
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public LogType Type { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; }

    public string Action { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    
}