namespace auth_template.Options;

public class KeyOptions
{
    public string AesKey { get; set; } = string.Empty;
    public string JwtSigningKey { get; set; } = string.Empty;
    public string HmacSecretKey { get; set; } = string.Empty;
}