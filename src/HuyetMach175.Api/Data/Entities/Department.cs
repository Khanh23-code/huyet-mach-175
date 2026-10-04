using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("departments")]
public class Department
{
    [Key]
    [Column("department_id")]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("department_code")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("department_name")]
    public string DepartmentName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
