using Microsoft.AspNetCore.Identity.UI.Services;

namespace FitTracker.Services;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Identity email queued for {Email}. Subject: {Subject}. Body: {Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
