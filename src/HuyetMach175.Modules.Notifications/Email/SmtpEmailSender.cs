using HuyetMach175.Modules.Notifications.Options;
using HuyetMach175.SharedKernel.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HuyetMach175.Modules.Notifications.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptionsMonitor<SmtpOptions> _smtpOptions;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptionsMonitor<SmtpOptions> smtpOptions,
        ILogger<SmtpEmailSender> logger)
    {
        _smtpOptions = smtpOptions;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var itemId = Guid.CreateVersion7();
        var results = await SendBatchAsync([new EmailBatchItem(itemId, message)], cancellationToken);
        var result = results.FirstOrDefault();

        if (result is null || !result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Gửi email tới '{message.To}' thất bại ({result?.ErrorCode ?? "UNKNOWN"}): {result?.ErrorMessage}");
        }
    }

    public async Task<IReadOnlyList<EmailBatchItemResult>> SendBatchAsync(
        IReadOnlyCollection<EmailBatchItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var options = _smtpOptions.CurrentValue;
        if (!options.IsConfigured)
        {
            return items
                .Select(i => new EmailBatchItemResult(
                    i.Id,
                    Succeeded: false,
                    IsPermanentFailure: false,
                    ErrorCode: "SMTP_NOT_CONFIGURED",
                    ErrorMessage: "Chưa cấu hình đầy đủ Notifications:Smtp (Host/FromAddress/Username/Password)."))
                .ToList();
        }

        MailboxAddress fromMailbox;
        try
        {
            fromMailbox = new MailboxAddress(options.FromName, options.FromAddress.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Địa chỉ FromAddress '{FromAddress}' không hợp lệ.", options.FromAddress);
            return items
                .Select(i => new EmailBatchItemResult(
                    i.Id,
                    Succeeded: false,
                    IsPermanentFailure: false,
                    ErrorCode: "INVALID_FROM_ADDRESS",
                    ErrorMessage: ex.Message))
                .ToList();
        }

        var results = new List<EmailBatchItemResult>(items.Count);
        using var transport = new SmtpClient
        {
            Timeout = Math.Max(5, options.TimeoutSeconds) * 1000
        };

        try
        {
            await ConnectAndAuthenticateAsync(transport, options, cancellationToken);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                MimeMessage mimeMessage;
                try
                {
                    mimeMessage = BuildMimeMessage(fromMailbox, item.Message);
                }
                catch (Exception ex) when (ex is ParseException or FormatException or ArgumentException)
                {
                    results.Add(new EmailBatchItemResult(
                        item.Id,
                        Succeeded: false,
                        IsPermanentFailure: true,
                        ErrorCode: "INVALID_RECIPIENT_ADDRESS",
                        ErrorMessage: ex.Message));
                    continue;
                }

                try
                {
                    if (!transport.IsConnected)
                    {
                        await ConnectAndAuthenticateAsync(transport, options, cancellationToken);
                    }

                    await transport.SendAsync(mimeMessage, cancellationToken);
                    results.Add(new EmailBatchItemResult(item.Id, Succeeded: true));
                }
                catch (SmtpCommandException ex)
                {
                    var statusCode = (int)ex.StatusCode;
                    var isPermanent = statusCode is >= 500 and < 600 &&
                                      ex.ErrorCode != SmtpErrorCode.UnexpectedStatusCode;

                    _logger.LogWarning(
                        ex,
                        "Lỗi lệnh SMTP ({StatusCode}) khi gửi email DeliveryId={DeliveryId} tới {To}.",
                        statusCode,
                        item.Id,
                        item.Message.To);

                    if (ex.ErrorCode == SmtpErrorCode.UnexpectedStatusCode)
                    {
                        await SafeDisconnectAsync(transport);
                    }

                    results.Add(new EmailBatchItemResult(
                        item.Id,
                        Succeeded: false,
                        IsPermanentFailure: isPermanent,
                        ErrorCode: $"SMTP_{statusCode}",
                        ErrorMessage: ex.Message));
                }
                catch (Exception ex) when (ex is SmtpProtocolException or IOException or TimeoutException or AuthenticationException)
                {
                    _logger.LogWarning(
                        ex,
                        "Lỗi kết nối/giao thức SMTP khi gửi email DeliveryId={DeliveryId} tới {To}. Sẽ đóng kết nối để mở lại sạch.",
                        item.Id,
                        item.Message.To);

                    await SafeDisconnectAsync(transport);

                    results.Add(new EmailBatchItemResult(
                        item.Id,
                        Succeeded: false,
                        IsPermanentFailure: false,
                        ErrorCode: ex.GetType().Name,
                        ErrorMessage: ex.Message));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Lỗi không xác định khi gửi email DeliveryId={DeliveryId} tới {To}.",
                        item.Id,
                        item.Message.To);

                    results.Add(new EmailBatchItemResult(
                        item.Id,
                        Succeeded: false,
                        IsPermanentFailure: false,
                        ErrorCode: "UNEXPECTED_SEND_ERROR",
                        ErrorMessage: ex.Message));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Không thể thiết lập kết nối ban đầu tới máy chủ SMTP {Host}:{Port}.", options.Host, options.Port);

            var processedIds = results.Select(r => r.Id).ToHashSet();
            foreach (var remaining in items.Where(i => !processedIds.Contains(i.Id)))
            {
                results.Add(new EmailBatchItemResult(
                    remaining.Id,
                    Succeeded: false,
                    IsPermanentFailure: false,
                    ErrorCode: "SMTP_CONNECT_FAILED",
                    ErrorMessage: ex.Message));
            }
        }
        finally
        {
            await SafeDisconnectAsync(transport);
        }

        return results;
    }

    private static async Task ConnectAndAuthenticateAsync(
        SmtpClient transport,
        SmtpOptions options,
        CancellationToken cancellationToken)
    {
        var secureSocketOptions = options.Security switch
        {
            SmtpSecurityMode.None => SecureSocketOptions.None,
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.StartTls
        };

        await transport.ConnectAsync(options.Host.Trim(), options.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
        {
            await transport.AuthenticateAsync(options.Username.Trim(), options.Password, cancellationToken);
        }
    }

    private static MimeMessage BuildMimeMessage(MailboxAddress fromMailbox, EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(fromMailbox);
        mime.To.Add(MailboxAddress.Parse(message.To.Trim()));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };
        mime.Body = builder.ToMessageBody();
        return mime;
    }

    private static async Task SafeDisconnectAsync(SmtpClient transport)
    {
        if (!transport.IsConnected)
        {
            return;
        }

        try
        {
            await transport.DisconnectAsync(quit: true, CancellationToken.None);
        }
        catch
        {
            // Bỏ qua lỗi đóng socket nếu kết nối đã ngắt từ phía server SMTP
        }
    }
}
