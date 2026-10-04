using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("role_permissions")]
public class RolePermission
{
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("permission_id")]
    public int PermissionId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [ForeignKey(nameof(PermissionId))]
    public Permission? Permission { get; set; }
}
