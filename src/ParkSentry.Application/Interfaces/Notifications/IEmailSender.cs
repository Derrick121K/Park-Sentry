namespace ParkSentry.Application.Interfaces.Notifications;

public interface IEmailSender
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public interface ISmsSender
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}

public record EmailMessage(string To, string Subject, string Body, bool IsHtml = false);
public record SmsMessage(string To, string Body);
