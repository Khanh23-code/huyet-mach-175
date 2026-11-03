using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.Processing;

public static class InventoryProcessing
{
    public static ComponentCode? MapComponentCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var clean = input.Trim().ToUpperInvariant();
        return clean switch
        {
            "WHOLE_BLOOD" or "WB" or "MÁU TOÀN PHẦN" => ComponentCode.WB,
            "RED_BLOOD_CELLS" or "PRBC" or "RBC" or "KHỐI HỒNG CẦU" or "HỒNG CẦU" => ComponentCode.PRBC,
            "PLASMA" or "FFP" or "HUYẾT TƯƠNG" or "HUYẾT TƯƠNG TƯƠI ĐÔNG LẠNH" => ComponentCode.FFP,
            "PLATELETS" or "PLT" or "TIỂU CẦU" or "KHỐI TIỂU CẦU" => ComponentCode.PLT,
            _ => Enum.TryParse<ComponentCode>(clean, true, out var parsed) ? parsed : null
        };
    }

    public static string FormatComponentCode(ComponentCode code) => code switch
    {
        ComponentCode.WB => "WHOLE_BLOOD",
        ComponentCode.PRBC => "RED_BLOOD_CELLS",
        ComponentCode.FFP => "PLASMA",
        ComponentCode.PLT => "PLATELETS",
        _ => code.ToString()
    };

    public static string ResolveEffectiveStatus(BagStatus status, DateTime expiredAt, DateTime now)
    {
        if (status == BagStatus.DISCARDED)
        {
            return "DISPOSAL_PENDING";
        }

        if (expiredAt < now && status != BagStatus.TRANSFUSED && status != BagStatus.ISSUED)
        {
            return "EXPIRED";
        }

        return status.ToString();
    }
}