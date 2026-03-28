namespace auth_template.Configuration;

public static class DeviceIdentifierConfig
{
    public static bool ShouldDisableIdentification(string path)
    {
        return path.Contains("/auth/login") ||
               path.Contains("/auth/register") ||
               path.Contains("/auth/logout") ||
               path.Contains("/auth/refresh");
    }
}