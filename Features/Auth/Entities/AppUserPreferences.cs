using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using auth_template.Entities.Data;

namespace auth_template.Features.Auth.Entities;

public class AppUserPreferences
{
    [Key] [ForeignKey(nameof(User))] public Guid UserId { get; set; }

    public AppUser User { get; set; }
    public string Locale { get; set; } = "en-US";
}