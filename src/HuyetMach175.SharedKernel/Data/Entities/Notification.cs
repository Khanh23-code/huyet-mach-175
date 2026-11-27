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
    public Guid? RecipientUserId { get; set; }

    [Column("target_role_id")]
    public Guid? TargetRoleId { get; set; }

    [Column("target_department_id")]
    public Guid? TargetDepartmentId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("type")]
    public NotificationType Type { get; set; } = NotificationType.INFO;

    [Column("reference_type")]
    public NotificationReferenceType? ReferenceType { get; set; }

    [Column("reference_id")]
    public Guid? ReferenceId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }

    [ForeignKey(nameof(TargetRoleId))]
    public Role? TargetRole { get; set; }

    [ForeignKey(nameof(TargetDepartmentId))]
    public Department? TargetDepartment { get; set; }
}
