using IdentityServerNET.Abstractions.EmailSender;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class NullEmailSender : ICustomEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Send {subject} mail to {email}: {htmlMessage}", subject, email, htmlMessage);

        return Task.CompletedTask;
    }
}
