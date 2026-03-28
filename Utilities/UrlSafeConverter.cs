using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace auth_template.Utilities;

public static class UrlSafeConverter
{
    public static string ToUrlSafe(string input)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(input));
    }

    public static string FromUrlSafe(string input)
    {
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(input));
    }
}