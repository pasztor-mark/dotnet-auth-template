namespace auth_template.Utilities.Extensions;

public static class SanitizationExtensions
{
    extension(string query)
    {
        public bool SanitizeQuery(out string sanitized, int maxLength = 64)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                sanitized = "";
                return false;
            }

            sanitized = query.Trim();
            if (sanitized.Length > maxLength) sanitized = sanitized.Substring(0, maxLength);

            sanitized = sanitized.Replace("%", "").Replace("_", "");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9\._\-]", "");
            return !string.IsNullOrWhiteSpace(sanitized);
        }
    }
}