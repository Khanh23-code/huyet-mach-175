using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.Inventory;
using HuyetMach175.Modules.Inventory.Processing;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;

    public InventoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventorySummaryResponse> GetInventorySummaryAsync(InventorySummaryFilterDto filter)
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(3);

        var query = _context.BloodBags
            .AsNoTracking()
            .Include(b => b.ComponentType)
            .AsQueryable();

        if (filter.StorageLocationId.HasValue)
        {
            query = query.Where(b => b.LocationId == filter.StorageLocationId.Value);
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
            var componentCode = InventoryProcessing.MapComponentCode(filter.ComponentType);
            if (componentCode.HasValue)
            {
                query = query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == componentCode.Value);
            }
        }

        if (filter.IsExpiringSoon == true)
        {
            query = query.Where(b => b.ExpiredAt >= now && b.ExpiredAt <= expiringThreshold);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var stStr = filter.Status.Trim().ToUpperInvariant();
            if (stStr is "EXPIRED")
            {
                query = query.Where(b => b.ExpiredAt < now && b.Status != BagStatus.DISCARDED && b.Status != BagStatus.TRANSFUSED && b.Status != BagStatus.ISSUED);
            }
            else if (stStr is "DISPOSAL_PENDING" or "DISCARDED")
            {
                query = query.Where(b => b.Status == BagStatus.DISCARDED || (b.ExpiredAt < now && b.Status != BagStatus.TRANSFUSED && b.Status != BagStatus.ISSUED));
            }
            else if (stStr is "TESTING")
            {
                query = query.Where(b => b.Status == BagStatus.READY_TESTING || b.Status == BagStatus.TESTED_PASSED);
            }
            else if (stStr is "PROCESSING")
            {
                query = query.Where(b => b.Status == BagStatus.PROCESSED);
            }
            else if (Enum.TryParse<BagStatus>(stStr, true, out var parsedStatus))
            {
                if (parsedStatus == BagStatus.AVAILABLE)
                {
                    query = query.Where(b => b.Status == BagStatus.AVAILABLE && b.ExpiredAt >= now);
                }
                else
                {
                    query = query.Where(b => b.Status == parsedStatus);
                }
            }
        }

        var bags = await query
            .Select(b => new
            {
                b.BagId,
                b.BloodType,
                b.RhFactor,
                ComponentCode = b.ComponentType != null ? b.ComponentType.TypeCode : ComponentCode.WB,
                ComponentTypeName = b.ComponentType != null ? b.ComponentType.TypeName : "Máu toàn phần",
                b.VolumeMl,
                b.ExpiredAt,
                b.Status
            })
            .ToListAsync();

        var totalBags = bags.Count;
        var totalVolumeMl = bags.Sum(b => b.VolumeMl);

        var availableBags = bags.Count(b => b.Status == BagStatus.AVAILABLE && b.ExpiredAt >= now);

        var expiringSoonBags = bags.Count(b =>
            (b.Status == BagStatus.AVAILABLE ||
             b.Status == BagStatus.READY_TESTING ||
             b.Status == BagStatus.TESTED_PASSED ||
             b.Status == BagStatus.LOCKED ||
             b.Status == BagStatus.RESERVED) &&
            b.ExpiredAt >= now &&
            b.ExpiredAt <= expiringThreshold);

        var disposalPendingBags = bags.Count(b =>
            b.Status == BagStatus.DISCARDED ||
            (b.ExpiredAt < now && b.Status != BagStatus.TRANSFUSED && b.Status != BagStatus.ISSUED));

        var testingOrProcessingBags = bags.Count(b =>
            b.Status == BagStatus.NEWLY_COLLECTED ||
            b.Status == BagStatus.READY_TESTING ||
            b.Status == BagStatus.TESTED_PASSED ||
            b.Status == BagStatus.PROCESSED);

        var overview = new InventoryOverviewDto
        {
            TotalBags = totalBags,
            TotalVolumeMl = totalVolumeMl,
            AvailableBags = availableBags,
            ExpiringSoonBags = expiringSoonBags,
            DisposalPendingBags = disposalPendingBags,
            TestingOrProcessingBags = testingOrProcessingBags
        };

        var breakdown = bags
            .GroupBy(b => new
            {
                b.BloodType,
                b.RhFactor,
                b.ComponentCode,
                b.ComponentTypeName,
                Status = InventoryProcessing.ResolveEffectiveStatus(b.Status, b.ExpiredAt, now)
            })
            .Select(g => new InventoryBreakdownItemDto
            {
                BloodType = g.Key.BloodType.ToString(),
                RhFactor = g.Key.RhFactor.ToString(),
                BloodGroup = $"{(g.Key.BloodType == BloodType.AB ? "AB" : g.Key.BloodType.ToString())}{(g.Key.RhFactor == RhFactor.POSITIVE ? "+" : "-")}",
                ComponentType = InventoryProcessing.FormatComponentCode(g.Key.ComponentCode),
                ComponentTypeName = g.Key.ComponentTypeName,
                Status = g.Key.Status,
                TotalBags = g.Count(),
                TotalVolumeMl = g.Sum(x => x.VolumeMl)
            })
            .OrderBy(i => i.BloodType)
            .ThenBy(i => i.RhFactor)
            .ThenBy(i => i.ComponentType)
            .ThenBy(i => i.Status)
            .ToList();

        return new InventorySummaryResponse
        {
            SummaryDate = now,
            Overview = overview,
            Breakdown = breakdown
        };
    }
}