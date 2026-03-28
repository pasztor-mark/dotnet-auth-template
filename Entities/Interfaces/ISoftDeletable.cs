namespace auth_template.Entities.Interfaces;

public interface ISoftDeletable
{
    public bool Flagged { get; set; }
    public DateTime? DeletedAt { get; set; }

    public void Reinitialize()
    {
        this.Flagged = false;
        this.DeletedAt = null;
    }
}