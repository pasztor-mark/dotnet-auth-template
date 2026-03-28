using System.Linq.Expressions;
using System.Text.RegularExpressions;
using auth_template.Configuration;
using auth_template.Entities;
using auth_template.Entities.Data;
using auth_template.Features.Email.Enums;
using auth_template.Features.Email.Options;
using auth_template.Options;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace auth_template.Features.Email.Utilities.Client;

public class EmailSenderClient: IEmailSenderClient
{
    private readonly SmtpClient _client = new();
    private readonly EmailOptions _options;
    private bool _connected = false;
    private readonly string _smtpPassword;
    private readonly ILogger<EmailSenderClient> _logger;
    private readonly AppDbContext _ctx;

    public EmailSenderClient(ILogger<EmailSenderClient> logger, IOptions<SecurityOptions> securityOptions,
        IOptions<EmailOptions> options, AppDbContext ctx)
    {
        _ctx = ctx;
        _logger = logger;
        _options = options.Value;
        _smtpPassword = securityOptions.Value.LocalSmtpPassword;
    }


    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connected)
            {
                _logger.LogInformation("Connecting to SMTP server {Host}:{Port}...", _options.Host, _options.Port);
                await _client.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.None,
                    cancellationToken); // PROD: use security measures
                _logger.LogInformation("Authenticating as {Username}...", _options.Address);
                await _client.AuthenticateAsync(_options.Address, _smtpPassword, cancellationToken);
                _connected = true;
                _logger.LogInformation("SMTP connection established and authenticated.");
            }

            _logger.LogInformation("Sending email to {To}...", string.Join(", ", message.To));
            await _client.SendAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email.");
            throw;
        }
    }

    public async Task SendSecurityUpdateAsync(string to, string reason,
        SecurityAlertSeverity severity = SecurityAlertSeverity.INFO, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(to, Regexes.Email)) return;
        await this.SendAsync(MimeMessages.GetSecurityAlertEmail(to, reason, severity));
    }

}