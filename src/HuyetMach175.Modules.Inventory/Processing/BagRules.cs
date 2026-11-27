using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Common;

namespace HuyetMach175.Modules.Inventory.Processing;

public static class BagRules
{
    public static string ExternalBarcode(DateTime collectedAtUtc, Guid bagId) =>
        $"EXT-{collectedAtUtc:yyyyMMdd}-{bagId.ToString("N")[^6..].ToUpperInvariant()}";

    public static string ChildBarcode(string parentBarcode, ComponentCode code) =>
        $"{parentBarcode}-{code}";

    public static bool IsStorageSuitable(BloodComponentType? componentType, decimal targetTemperature) =>
        BloodComponentStorageRules.IsStorageSuitable(componentType, targetTemperature);

    public static string StorageHint(BloodComponentType? componentType) =>
        BloodComponentStorageRules.StorageHint(componentType);

    public static bool IsStorageSuitable(ComponentCode code, decimal targetTemperature) =>
        BloodComponentStorageRules.IsStorageSuitable(code, targetTemperature);

    public static string StorageHint(ComponentCode code) =>
        BloodComponentStorageRules.StorageHint(code);
}