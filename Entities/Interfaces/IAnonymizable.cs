
namespace auth_template.Entities.Interfaces;

public interface IAnonymizable
{
    public bool Flagged { get; set; }
    public DateTime? AnonymizedAt { get; set; }
    void Anonymize();
}