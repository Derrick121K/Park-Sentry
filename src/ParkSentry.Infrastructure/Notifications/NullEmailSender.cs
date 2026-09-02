using Microsoft.Extensions.Logging;
using ParkSentry.Application.Interfaces.Notifications;

namespace ParkSentry.Infrastructure.Notifications;

public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public string ProviderName => "Null";
    public bool IsConfigured => false;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Email not configured; suppressed message to {To} subject {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}

public sealed class NullSmsSender(ILogger<NullSmsSender> logger) : ISmsSender
{
    public string ProviderName => "Null";
    public bool IsConfigured => false;

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("SMS not configured; suppressed message to {To}", message.To);
        return Task.CompletedTask;
    }
}
