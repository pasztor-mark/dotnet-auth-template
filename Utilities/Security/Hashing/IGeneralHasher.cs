namespace auth_template.Utilities.Security.Hashing;

public interface IGeneralHasher
{
    string ComputeHmacSha256(string input);
    bool VerifyHash(string input, string expectedHmac);
}