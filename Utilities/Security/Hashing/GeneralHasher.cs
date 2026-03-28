using System.Security.Cryptography;
using System.Text;
using auth_template.Options;
using Microsoft.Extensions.Options;

namespace auth_template.Utilities.Security.Hashing;

public class GeneralHasher : IGeneralHasher
{

    private readonly byte[] secretKey;

    public GeneralHasher(IOptions<SecurityOptions> options)
    {
        var keyFromConfig = options.Value.Keys.HmacSecretKey;
        if (string.IsNullOrEmpty(keyFromConfig))
            throw new ArgumentNullException("HMAC Key Missing");
        secretKey = Convert.FromBase64String(keyFromConfig);
    }

    public string ComputeHmacSha256(string input)
    {
        if (secretKey is null || secretKey.Length == 0)
            throw new InvalidOperationException("Secret key is not initialized.");
        using var hmac = new HMACSHA256(secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHash(string input, string expectedHmac)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(expectedHmac))
            return false;

        try
        {
            using var hmac = new HMACSHA256(secretKey);
            var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            var expectedBytes = Convert.FromBase64String(expectedHmac);

            return CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
        }
        catch
        {
            return false;
        }
    }
}

