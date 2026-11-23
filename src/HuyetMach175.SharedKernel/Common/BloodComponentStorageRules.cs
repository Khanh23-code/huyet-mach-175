using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.SharedKernel.Common;

public static class BloodComponentStorageRules
{
    public static bool IsStorageSuitable(BloodComponentType? componentType, decimal targetTemperature)
    {
        if (componentType != null && HasConfiguredTempRange(componentType))
        {
            return targetTemperature >= componentType.MinTemp && targetTemperature <= componentType.MaxTemp;
        }

        return IsStorageSuitable(componentType?.TypeCode ?? ComponentCode.WB, targetTemperature);
    }

    public static string StorageHint(BloodComponentType? componentType)
    {
        if (componentType != null && HasConfiguredTempRange(componentType))
        {
            return $"{componentType.MinTemp:0.#}°C đến {componentType.MaxTemp:0.#}°C";
        }

        return StorageHint(componentType?.TypeCode ?? ComponentCode.WB);
    }

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

    private static bool HasConfiguredTempRange(BloodComponentType componentType) =>
        componentType.MinTemp <= componentType.MaxTemp &&
        (componentType.MinTemp != 0m || componentType.MaxTemp != 0m);
}
