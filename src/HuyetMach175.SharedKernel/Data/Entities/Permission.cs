using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("permissions")]
public class Permission
{
    [Key]
    [Column("permission_id")]
    public int PermissionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("permission_code")]
    public string PermissionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("permission_name")]
    public string PermissionName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
