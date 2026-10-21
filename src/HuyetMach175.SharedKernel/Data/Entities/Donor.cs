using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("donors")]
public class Donor
{
    [Key]
    [Column("donor_id")]
    public int DonorId { get; set; }

    [Required]
    [MaxLength(12)]
    [Column("citizen_id")]
    public string CitizenId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public DateOnly DateOfBirth { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("gender")]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("blood_type")]
    public BloodType? BloodType { get; set; }

    [Column("rh_factor")]
    public RhFactor? RhFactor { get; set; }

    [Column("total_donations")]
    public int TotalDonations { get; set; } = 0;

    [Column("last_donation_date")]
    public DateOnly? LastDonationDate { get; set; }

    public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
}
