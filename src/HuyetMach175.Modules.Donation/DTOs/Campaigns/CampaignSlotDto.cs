namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class CampaignSlotDto
{
    public int SlotId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Capacity { get; set; }
}

public class CreateCampaignSlotDto
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Capacity { get; set; } = 20;
}