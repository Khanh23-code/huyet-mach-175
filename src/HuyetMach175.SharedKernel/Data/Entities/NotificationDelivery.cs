using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("notification_delivery")]
public class NotificationDelivery
{
    [Key]
    [Column("delivery_id")]
    public Guid DeliveryId { get; set; } = Guid.CreateVersion7();

    [Column("notification_id")]
    public Guid? NotificationId { get; set; }

    [Column("donor_id")]
    public Guid? DonorId { get; set; }

    [Column("channel")]
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;

    [Required]
    [MaxLength(256)]
    [Column("destination")]
    public string Destination { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("deduplication_key")]
    public string? DeduplicationKey { get; set; }

    [Column("status")]
    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;

    [Column("attempt_count")]
    public int AttemptCount { get; set; } = 0;

    [Column("next_attempt_at")]
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    [Column("last_error_code")]
    public string? LastErrorCode { get; set; }

    [Column("last_error_message")]
    public string? LastErrorMessage { get; set; }

    [Column("payload_json", TypeName = "jsonb")]
    public string? PayloadJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [ForeignKey(nameof(NotificationId))]
    public Notification? Notification { get; set; }

    [ForeignKey(nameof(DonorId))]
    public Donor? Donor { get; set; }
}
