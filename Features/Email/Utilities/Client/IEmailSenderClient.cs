using System.Linq.Expressions;
using auth_template.Entities.Data;
using auth_template.Features.Email.Enums;
using MimeKit;

namespace auth_template.Features.Email.Utilities.Client;

public interface IEmailSenderClient
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default);

    Task SendSecurityUpdateAsync(string to, string reason,
        SecurityAlertSeverity severity = SecurityAlertSeverity.INFO, CancellationToken cancellationToken = default);

}