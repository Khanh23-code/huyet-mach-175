using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.Processing;

public static class BagRules
{
    public static string ExternalBarcode(DateTime collectedAtUtc, int bagId) =>
        $"EXT-{collectedAtUtc:yyyyMMdd}-{bagId:D6}";

    public static string ChildBarcode(string parentBarcode, ComponentCode code) =>
        $"{parentBarcode}-{code}";

    public static bool IsStorageSuitable(ComponentCode code, decimal targetTemperature) => code switch
    {
        ComponentCode.WB or ComponentCode.PRBC => targetTemperature is >= 1m and <= 10m,
        ComponentCode.FFP => targetTemperature <= -18m,
        ComponentCode.PLT => targetTemperature is >= 20m and <= 24m,
        _ => true
    };

    public static string StorageHint(ComponentCode code) => code switch
    {
        ComponentCode.WB or ComponentCode.PRBC => "1°C đến 10°C",
        ComponentCode.FFP => "-18°C hoặc thấp hơn",
        ComponentCode.PLT => "20°C đến 24°C",
        _ => string.Empty
    };
}