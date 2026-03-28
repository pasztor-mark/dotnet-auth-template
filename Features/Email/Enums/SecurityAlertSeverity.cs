namespace auth_template.Features.Email.Enums;

public enum SecurityAlertSeverity
{
    INFO,
    WARNING,
    SEVERE
}

public static class SeverityFunctions
{
    public static string GetSecurityAlertSubject(SecurityAlertSeverity severity, string reason)
    {
        string prefix = severity switch
        {
            SecurityAlertSeverity.SEVERE => "⚠️ CRITICAL: Security Alert",
            SecurityAlertSeverity.WARNING => "🔔 Warning: Security Notice",
            SecurityAlertSeverity.INFO => "ℹ️ Account activity notice:",
            _ => "Security Notice"
        };

        return $"{prefix} - {reason}";
    }

    public static (string Color, string Title) GetVisualsBySeverity(SecurityAlertSeverity severity)
    {
        return severity switch
        {
            SecurityAlertSeverity.INFO => ("#3b82f6", "INFO"),
            SecurityAlertSeverity.WARNING => ("#f59e0b", "WARNING"),
            SecurityAlertSeverity.SEVERE => ("#ef4444", "CRITICAL ALERT"),
            _ => ("#6b7280", "NOTICE")
        };
    }

    public static class EmailTemplateUtils
    {
        public static string GetSecurityAlertBody(SecurityAlertSeverity severity, string reason, string actionUrl)
        {
            var (headerColor, title, buttonColor) = severity switch
            {
                SecurityAlertSeverity.INFO => ("#3b82f6", "INFORMATION", "#2563eb"),
                SecurityAlertSeverity.WARNING => ("#f59e0b", "WARNING", "#d97706"),
                SecurityAlertSeverity.SEVERE => ("#ef4444", "CRITICAL ALERT", "#dc2626"),
                _ => ("#6b7280", "SECURITY NOTICE", "#4b5563")
            };

            return $@"
        <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 20px auto; padding: 0; border: 1px solid #e5e7eb; border-radius: 12px; overflow: hidden; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
            
            <div style=""padding: 20px; background-color: {headerColor}; color: #ffffff; text-align: center; font-weight: 700; font-size: 20px; letter-spacing: 0.05em;"">
                {title}
            </div>

            <div style=""padding: 32px 24px; text-align: left;"">
                <h2 style=""color: #111827; margin: 0 0 16px 0; font-size: 22px; font-weight: 700;"">Security Notification</h2>
                
                <p style=""font-size: 16px; line-height: 1.6; color: #4b5563; margin-bottom: 24px;"">
                    An important event has occurred regarding your account on <strong>ECC Course Platform</strong>:<br>
                    <span style=""display: block; margin-top: 12px; padding: 12px; background-color: #f9fafb; border-left: 4px solid {headerColor}; color: #1f2937; font-weight: 500;"">
                        {reason}
                    </span>
                </p>

                <p style=""font-size: 14px; line-height: 1.5; color: #6b7280; margin-bottom: 32px;"">
                    If you did not authorize this action, please secure your account immediately or contact us.
                </p>

                <div style=""text-align: center;"">
                    <a href=""{actionUrl}"" style=""background-color: {buttonColor}; color: #ffffff; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; display: inline-block;"">
                        Review Activity
                    </a>
                </div>
            </div>

            <div style=""padding: 20px; text-align: center; font-size: 12px; color: #9ca3af; background-color: #f9fafb; border-top: 1px solid #e5e7eb;"">
                © 2026  All rights reserved.<br>
                This is an automated security notification.
            </div>
        </div>";
        }
    }
}