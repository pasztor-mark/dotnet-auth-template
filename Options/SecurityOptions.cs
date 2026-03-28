namespace auth_template.Options;

public class SecurityOptions
{
    public int CurrentVersion { get; set; }
    
    public Dictionary<string, string> BlindIndexPeppers { get; set; } = new();
    
    public string IssuerAudiencePair { get; set; } = string.Empty;
    public string LocalSmtpPassword { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;

    public KeyOptions Keys { get; set; } = new();
}