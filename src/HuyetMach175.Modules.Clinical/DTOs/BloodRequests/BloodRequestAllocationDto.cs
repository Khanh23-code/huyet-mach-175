using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class BloodRequestAllocationDto
{
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int ComponentTypeId { get; set; }
    public ComponentCode? ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public BloodType? BloodType { get; set; }
    public RhFactor? RhFactor { get; set; }
    public int VolumeMl { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public BagStatus? BagStatus { get; set; }
    public AllocationStatus Status { get; set; }
    public DateTime ReservedAt { get; set; }
    public string? ReservedByName { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? IssuedByName { get; set; }
    public string? ReceivedByName { get; set; }
}
