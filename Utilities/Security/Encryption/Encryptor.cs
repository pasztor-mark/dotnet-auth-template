using System.Security.Cryptography;
using System.Text;
using auth_template.Options;
using auth_template.Utilities.Security.Pepper;
using Microsoft.Extensions.Options;

namespace auth_template.Utilities.Security.Encryption;

public class Encryptor : IEncryptor
{
    private readonly byte[] _aesKey;
    private readonly IPepperProvider _pepperProvider;

    public Encryptor(IPepperProvider pepperProvider, IOptions<SecurityOptions> _options)
    {
        var keyFromConfig = _options.Value.Keys.AesKey ?? throw new ArgumentNullException("AesKey missing");
        _aesKey = Convert.FromBase64String(keyFromConfig);

        if (_aesKey.Length != 32)
            throw new ArgumentException("AES Key must be 256 bits (32 bytes) for AES-256.");
        _pepperProvider = pepperProvider;

    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        aes.IV = RandomNumberGenerator.GetBytes(16);

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var combined = new byte[16 + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, 16);
        Buffer.BlockCopy(encryptedBytes, 0, combined, 16, encryptedBytes.Length);
        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string cipherText)
    {
        try
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            byte[] fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[16];

            if (fullCipher.Length < 16) throw new CryptographicException("Invalid cipher text.");

            var cipherBytes = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception e)
        {
            return string.Empty;
        }
    }

    public string GenerateBlindIndex(string input, int? version = null)
    {
        if (string.IsNullOrEmpty(input)) return input;
        int v = version ?? _pepperProvider.GetCurrentVersion();
        byte[] currentPepper = _pepperProvider.GetPepper(v);
        using var hmac = new HMACSHA256(currentPepper);
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));

        return Convert.ToHexString(hashBytes);
    }
}