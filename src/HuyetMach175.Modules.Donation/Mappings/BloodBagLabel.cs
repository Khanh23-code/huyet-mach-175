using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class BloodBagLabel
{
    public static string GroupLabel(BloodType bt, RhFactor rh) =>
        $"{bt}{(rh == RhFactor.POSITIVE ? "+" : "-")}";

    public static string? RhLabel(RhFactor? rh) => rh switch
    {
        RhFactor.POSITIVE => "Rh+",
        RhFactor.NEGATIVE => "Rh-",
        _ => null
    };

    public static int VolumeMl(TargetVolume v) => v switch
    {
        TargetVolume.VOL_250 => 250,
        TargetVolume.VOL_350 => 350,
        TargetVolume.VOL_450 => 450,
        _ => 0
    };

    public static string BuildBarcode(
        BloodType bt,
        RhFactor rh,
        int volumeMl,
        DateTime collectedAtUtc,
        int sessionId) =>
        $"BAG-{collectedAtUtc:yyyyMMdd}-{sessionId}";
}