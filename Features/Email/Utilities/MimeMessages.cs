using System.Net;
using auth_template.Features.Email.Enums;
using MimeKit;

namespace auth_template.Features.Email.Utilities;

public static class MimeMessages
{
    private const string AppName = "Template Application";
    private const string AppEmail = "noreply@template.local";
    private const string FooterText = "Template Application, © 2026.";

    public static MimeMessage GetConfirmEmailMessage(string email, string confirmLink)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Confirm Your Email Address";
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html><html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #1a73e8; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>Confirm your E-mail</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      You have requested to confirm your email on {AppName}. Please click on the button below to confirm your E-mail address.
    </p>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>This email is for {email}. If you didn't request a confirmation, please ignore this email. Please do not reply to this e-mail.</p>

    <table border='0' cellpadding='0' cellspacing='0' style='margin: 24px auto; width: auto;'>
      <tr>
        <td align='center' bgcolor='#1a73e8' style='border-radius: 24px;'>
          <a href='{confirmLink}' target='_blank' style='display: inline-block; padding: 12px 32px; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; font-size: 15px; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 24px; border: 1px solid #1a73e8;'>
            CONFIRM
          </a>
        </td>
      </tr>
    </table>
    
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Regards,<br>
      {FooterText}
    </p>
  </div>
</body>
</html>
"
        };
        return message;
    }

    public static MimeMessage GetForgotPassword(string email, string resetLink)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Recover Your Password";
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #1a73e8; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>Password Recovery</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      You have requested to recover your forgotten password on {AppName}. Please click on the button below to set a new password.
    </p>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>This email is for {email}. If you didn't request a password reset, please ignore this email or contact support immediately. Please do not reply to this e-mail.</p>
    
    <table border='0' cellpadding='0' cellspacing='0' style='margin: 24px auto; width: auto;'>
      <tr>
        <td align='center' bgcolor='#1a73e8' style='border-radius: 24px;'>
          <a href='{resetLink}' target='_blank' style='display: inline-block; padding: 12px 32px; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; font-size: 15px; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 24px; border: 1px solid #1a73e8;'>
            Reset Password
          </a>
        </td>
      </tr>
    </table>
    
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Regards,<br>
      {FooterText}
    </p>
  </div>
</body>
</html>
"
        };
        return message;
    }

    public static MimeMessage GetEmailChange(string oldEmail, string newEmail, string confirmLink)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", newEmail));
        message.Subject = "Change Your E-mail Address";
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #1a73e8; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>E-mail Change</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      You have requested to change your E-mail address on {AppName}. Please click on the button below to confirm your new address.
    </p>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>This email is for {newEmail}. If you didn't request an e-mail change, please ignore this email or contact support immediately. Please do not reply to this e-mail.</p>
    
    <table border='0' cellpadding='0' cellspacing='0' style='margin: 24px auto; width: auto;'>
      <tr>
        <td align='center' bgcolor='#1a73e8' style='border-radius: 24px;'>
          <a href='{confirmLink}' target='_blank' style='display: inline-block; padding: 12px 32px; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; font-size: 15px; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 24px; border: 1px solid #1a73e8;'>
            Confirm Change
          </a>
        </td>
      </tr>
    </table>
    
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Regards,<br>
      {FooterText}
    </p>
  </div>
</body>
</html>
"
        };
        return message;
    }

    public static MimeMessage GetSecurityAlertEmail(string to, string reason, SecurityAlertSeverity severity)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = SeverityFunctions.GetSecurityAlertSubject(severity, reason);
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #1a73e8; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>Security Alert</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      {System.Net.WebUtility.HtmlEncode(SeverityFunctions.GetSecurityAlertSubject(severity, reason))}
    </p>
    
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Regards,<br>
      {FooterText}
    </p>
  </div>
</body>
</html>
"
        };
        return message;
    }

    public static MimeMessage GetReactivateAccountEmail(string email, string resetLink)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Set Password for your Reactivated Account";
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #1a73e8; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>Account Recovery</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      You have requested to recover your account on {AppName}. Please click on the button below to set a new password for your account.
    </p>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>This email is for {email}. If you didn't authorize this action, please ignore this email and contact support immediately. Please do not reply to this e-mail.</p>
    
    <table border='0' cellpadding='0' cellspacing='0' style='margin: 24px auto; width: auto;'>
      <tr>
        <td align='center' bgcolor='#1a73e8' style='border-radius: 24px;'>
          <a href='{resetLink}' target='_blank' style='display: inline-block; padding: 12px 32px; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; font-size: 15px; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 24px; border: 1px solid #1a73e8;'>
            Reset Password
          </a>
        </td>
      </tr>
    </table>
    
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Regards,<br>
      {FooterText}
    </p>
  </div>
</body>
</html>
"
        };
        return message;
    }

    public static MimeMessage GetAccountBan(string email, string reason)
    {
        MimeMessage message = new();
        var safeReason = WebUtility.HtmlEncode(reason);
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Notice of Account Suspension";
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #ef4444; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>Account Suspended</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      This is an automated notification to inform you that your account on {AppName} has been suspended due to a violation of our terms of service or community guidelines.
    </p>
    <div style='background: #171b26; padding: 16px 24px; border-left: 4px solid #ef4444; border-radius: 6px; margin: 24px auto; text-align: left; width: 80%;'>
      <p style='margin: 0; font-size: 14px; color: #ef4444; font-weight: 600;'>Reason for suspension:</p>
      <p style='margin: 8px 0 0 0; font-size: 15px; color: #ffffff;'>{safeReason}</p>
    </div>
    <p style='font-size: 14px; line-height: 1.6; margin-bottom: 16px; color: #9ca3af;'>
      If you believe this suspension is an error, please contact our support team to appeal the decision.
    </p>
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Regards,<br>
      {FooterText}
    </p>
  </div>
</body>
</html>"
        };
        return message;
    }

    public static MimeMessage GetPaymentSuccess(string email, string tier)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(AppName, AppEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Subscription Confirmed";
        message.Body = new TextPart("html")
        {
            Text = $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
</head>
<body style='margin: 0; padding: 0; background: #171b26; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; color: #e5e7eb;'>
  <div style='max-width: 600px; margin: 40px auto; background: #202532; padding: 40px; border-radius: 12px; border: 1px solid #2d3343; text-align: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);'>
    <h1 style='font-size: 24px; margin-bottom: 8px; color: #ffffff;'>{AppName}</h1>
    <h3 style='color: #1a73e8; font-size: 18px; margin-top: 0; margin-bottom: 24px;'>Subscription Active</h3>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      Thank you! Your transaction has been successfully processed. 
    </p>
    <p style='font-size: 15px; line-height: 1.6; margin-bottom: 16px; color: #cbd5e1;'>
      Your account has been upgraded to the <strong>{tier}</strong> tier. You now have access to all the exclusive features included in your subscription plan.
    </p>
    
    <table border='0' cellpadding='0' cellspacing='0' style='margin: 24px auto; width: auto;'>
      <tr>
        <td align='center' bgcolor='#1a73e8' style='border-radius: 24px;'>
          <a href='#' target='_blank' style='display: inline-block; padding: 12px 32px; font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif; font-size: 15px; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 24px; border: 1px solid #1a73e8;'>
            Go to Dashboard
          </a>
        </td>
      </tr>
    </table>
    
    <p style='font-size: 14px; line-height: 1.6; margin-bottom: 16px; color: #9ca3af;'>
      We are thrilled to have you on board. If you have any questions regarding your subscription or billing, feel free to contact support.
    </p>
    <p style='font-size: 14px; line-height: 1.6; margin-top: 32px; margin-bottom: 0; color: #64748b;'>
      Welcome!<br>
      {FooterText}
    </p>
  </div>
</body>
</html>"
        };
        return message;
    }
}