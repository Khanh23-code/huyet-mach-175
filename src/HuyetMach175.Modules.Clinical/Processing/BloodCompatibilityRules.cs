using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.Processing;

public static class BloodCompatibilityRules
{
    public static readonly (BloodType BloodType, RhFactor RhFactor)[] StandardBloodGroups =
    {
        (BloodType.O, RhFactor.POSITIVE),
        (BloodType.O, RhFactor.NEGATIVE),
        (BloodType.A, RhFactor.POSITIVE),
        (BloodType.A, RhFactor.NEGATIVE),
        (BloodType.B, RhFactor.POSITIVE),
        (BloodType.B, RhFactor.NEGATIVE),
        (BloodType.AB, RhFactor.POSITIVE),
        (BloodType.AB, RhFactor.NEGATIVE)
    };

    public static string FormatBloodGroup(BloodType bloodType, RhFactor rhFactor)
    {
        var rhSymbol = rhFactor == RhFactor.POSITIVE ? "+" : "-";
        return $"{bloodType}{rhSymbol}";
    }

    public static bool IsExactMatch(BloodType recipientBloodType, RhFactor recipientRh, BloodType donorBloodType, RhFactor donorRh)
    {
        return recipientBloodType == donorBloodType && recipientRh == donorRh;
    }

    public static bool IsCompatible(
        ComponentCode componentCode,
        BloodType recipientBloodType,
        RhFactor recipientRh,
        BloodType donorBloodType,
        RhFactor donorRh)
    {
        if (IsExactMatch(recipientBloodType, recipientRh, donorBloodType, donorRh))
        {
            return true;
        }

        return componentCode switch
        {
            ComponentCode.WB or ComponentCode.PRBC => IsRbcCompatible(recipientBloodType, recipientRh, donorBloodType, donorRh),
            ComponentCode.FFP => IsPlasmaAboCompatible(recipientBloodType, donorBloodType),
            ComponentCode.PLT => IsPlasmaAboCompatible(recipientBloodType, donorBloodType) && IsRhCompatible(recipientRh, donorRh),
            _ => false
        };
    }

    public static int GetUrgencyRank(UrgencyLevel urgencyLevel) => urgencyLevel switch
    {
        UrgencyLevel.EMERGENCY => 0,
        UrgencyLevel.URGENT => 1,
        UrgencyLevel.ROUTINE => 2,
        _ => 3
    };

    private static bool IsRbcCompatible(BloodType recipientBloodType, RhFactor recipientRh, BloodType donorBloodType, RhFactor donorRh)
    {
        if (!IsRhCompatible(recipientRh, donorRh))
        {
            return false;
        }

        return recipientBloodType switch
        {
            BloodType.O => donorBloodType == BloodType.O,
            BloodType.A => donorBloodType is BloodType.A or BloodType.O,
            BloodType.B => donorBloodType is BloodType.B or BloodType.O,
            BloodType.AB => true,
            _ => false
        };
    }

    private static bool IsPlasmaAboCompatible(BloodType recipientBloodType, BloodType donorBloodType)
    {
        return recipientBloodType switch
        {
            BloodType.O => true,
            BloodType.A => donorBloodType is BloodType.A or BloodType.AB,
            BloodType.B => donorBloodType is BloodType.B or BloodType.AB,
            BloodType.AB => donorBloodType == BloodType.AB,
            _ => false
        };
    }

    private static bool IsRhCompatible(RhFactor recipientRh, RhFactor donorRh)
    {
        return recipientRh == RhFactor.POSITIVE || donorRh == RhFactor.NEGATIVE;
    }
}
