using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Mappings;

public static class BloodBagFilterExtensions
{
    public static IQueryable<BloodBag> ApplyFilter(this IQueryable<BloodBag> query, BloodBagFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var statusStr = filter.Status.Trim().ToUpperInvariant();
            if (statusStr == "TESTING")
            {
                query = query.Where(b => b.Status == BagStatus.READY_TESTING);
            }
            else if (Enum.TryParse<BagStatus>(statusStr, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.BloodType))
        {
            var btStr = filter.BloodType.Trim().ToUpperInvariant();
            if (Enum.TryParse<BloodType>(btStr, true, out var parsedBloodType))
            {
                query = query.Where(b => b.BloodType == parsedBloodType);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.RhFactor))
        {
            var rhStr = filter.RhFactor.Trim().ToUpperInvariant();
            if (rhStr is "+" or "POS" or "POSITIVE")
            {
                query = query.Where(b => b.RhFactor == RhFactor.POSITIVE);
            }
            else if (rhStr is "-" or "NEG" or "NEGATIVE")
            {
                query = query.Where(b => b.RhFactor == RhFactor.NEGATIVE);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ComponentType))
        {
            var ctStr = filter.ComponentType.Trim().ToUpperInvariant();
            query = ctStr switch
            {
                "WHOLE_BLOOD" or "WB" => query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.WB),
                "PACKED_RED_BLOOD_CELLS" or "PRBC" => query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.PRBC),
                "FRESH_FROZEN_PLASMA" or "FFP" => query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.FFP),
                "PLATELETS" or "PLT" => query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.PLT),
                _ => Enum.TryParse<ComponentCode>(ctStr, true, out var parsedCode)
                    ? query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == parsedCode)
                    : query.Where(b => b.ComponentType != null && EF.Functions.ILike(b.ComponentType.TypeName, $"%{filter.ComponentType.Trim()}%"))
            };
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(b => EF.Functions.ILike(b.Barcode, $"%{kw}%"));
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(b => b.LocationId == filter.LocationId.Value);
        }

        return query;
    }
}