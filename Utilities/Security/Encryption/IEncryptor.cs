namespace auth_template.Utilities.Security.Encryption;

public interface IEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string GenerateBlindIndex(string input, int? version);
}