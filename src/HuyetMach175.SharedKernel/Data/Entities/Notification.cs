using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("notification")]
public class Notification
{
    [Key]
    [Column("notification_id")]
    public Guid NotificationId { get; set; } = Guid.CreateVersion7();

    [Column("recipient_user_id")]
    public Guid RecipientUserId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("notification_type")]
    public string NotificationType { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("source_module")]
    public string SourceModule { get; set; } = "Notifications";

    [Column("source_event_id")]
    public Guid? SourceEventId { get; set; }

    [MaxLength(200)]
    [Column("deduplication_key")]
    public string? DeduplicationKey { get; set; }

    [MaxLength(64)]
    [Column("resource_type")]
    public string? ResourceType { get; set; }

    [Column("resource_id")]
    public Guid? ResourceId { get; set; }

    [MaxLength(500)]
    [Column("action_path")]
    public string? ActionPath { get; set; }

    [Column("priority")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }

    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}
