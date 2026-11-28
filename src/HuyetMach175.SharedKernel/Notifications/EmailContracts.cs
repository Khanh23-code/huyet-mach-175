namespace HuyetMach175.SharedKernel.Notifications;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);

public sealed record EmailBatchItem(
    Guid Id,
    EmailMessage Message);

public sealed record EmailBatchItemResult(
    Guid Id,
    bool Succeeded,
    bool IsPermanentFailure = false,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailBatchItemResult>> SendBatchAsync(
        IReadOnlyCollection<EmailBatchItem> items,
        CancellationToken cancellationToken = default);
}
