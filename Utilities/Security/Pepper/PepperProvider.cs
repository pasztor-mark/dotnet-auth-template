using System.Security.Cryptography;
using auth_template.Options;
using Microsoft.Extensions.Options;

namespace auth_template.Utilities.Security.Pepper;

public class PepperProvider(IOptions<SecurityOptions> options) : IPepperProvider
{
    public byte[] GetPepper(int version)
    {
        if (options.Value.BlindIndexPeppers.TryGetValue(version.ToString(), out var pepper))
        {
            return Convert.FromHexString(pepper); 
        }
        throw new CryptographicException($"Pepper version {version} not found in Key Ring.");
    }

    public int GetCurrentVersion() => options.Value.CurrentVersion;
}