using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_test_results")]
public class BloodTestResult
{
    [Key]
    [Column("test_id")]
    public int TestId { get; set; }

    [Column("bag_id")]
    public int BagId { get; set; }

    [Column("technician_id")]
    public int TechnicianId { get; set; }

    [Column("hiv_result")]
    public TestResult HivResult { get; set; }

    [Column("hbv_result")]
    public TestResult HbvResult { get; set; }

    [Column("hcv_result")]
    public TestResult HcvResult { get; set; }

    [Column("syphilis_result")]
    public TestResult SyphilisResult { get; set; }

    [Column("irregular_antibody")]
    public TestResult IrregularAntibody { get; set; } = TestResult.NEGATIVE;

    [Column("confirmed_blood_type")]
    public BloodType ConfirmedBloodType { get; set; }

    [Column("confirmed_rh")]
    public RhFactor ConfirmedRh { get; set; }

    [Column("overall_conclusion")]
    public OverallConclusion OverallConclusion { get; set; }

    [Column("tested_at")]
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BagId))]
    public BloodBag? Bag { get; set; }

    [ForeignKey(nameof(TechnicianId))]
    public User? Technician { get; set; }
}
