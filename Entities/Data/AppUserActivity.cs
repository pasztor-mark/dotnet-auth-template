using System.ComponentModel.DataAnnotations.Schema;

namespace auth_template.Entities.Data;

public class AppUserActivity
{
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        public DateTime Date { get; set; } 
        public string PageKey { get; set; } 

        public int TotalSeconds { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
