using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace auth_template.Entities.Data;

public class AppUserTagAssignment
{
    [Key]
    public Guid Id { get; set; }
    
    public AppUser User { get; set; }
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }
    public AppUserTag Tag { get; set; }
    public DateTime AssignedAt { get; set; }
    public string Reason { get; set; }
    
    public Guid? AssignedById { get; set; }
    [ForeignKey(nameof(AssignedById))]
    public AppUser? AssignedBy { get; set; }
    
    
}