using System.Globalization;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Donation.DTOs.Sites;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;

namespace HuyetMach175.Modules.Donation.Services;

internal static class SiteMapper
{
    private const string TimeFormat = "HH:mm";

    public static string Fmt(TimeOnly t) => t.ToString(TimeFormat, CultureInfo.InvariantCulture);

    public static TimeOnly Parse(string s) => TimeOnly.ParseExact(s, TimeFormat, CultureInfo.InvariantCulture);

    public static List<SlotDto> ToSlotDtos(IEnumerable<DonationSiteSlot> slots) =>
        slots.OrderBy(s => s.StartTime)
            .Select(s => new SlotDto { StartTime = Fmt(s.StartTime), EndTime = Fmt(s.EndTime), Capacity = s.Capacity })
            .ToList();

    public static DonationSiteSlot ToEntity(SlotDto d, int siteId = 0) => new()
    {
        SiteId = siteId,
        StartTime = Parse(d.StartTime),
        EndTime = Parse(d.EndTime),
        Capacity = d.Capacity
    };

    public static SiteDto ToDto(DonationSite s)
    {
        var slots = ToSlotDtos(s.Slots);
        return new SiteDto
        {
            SiteId = s.SiteId,
            SiteCode = s.SiteCode ?? string.Empty,
            SiteName = s.SiteName,
            SiteType = s.SiteType.ToString(),
            Address = s.Address,
            PhoneNumber = s.PhoneNumber,
            IsActive = s.IsActive,
            TotalCapacityPerDay = slots.Sum(x => x.Capacity),
            Slots = slots,
            CreatedAt = s.CreatedAt
        };
    }
}

internal static class SlotValidator
{
    private const int MaxSlots = 12;

    /// <summary>Kiểm tra định dạng, khoảng giờ, sức chứa, chồng lấn; trả về danh sách đã sắp xếp theo giờ bắt đầu.</summary>
    public static List<SlotDto> ValidateAndNormalize(IEnumerable<SlotInput>? input)
    {
        var list = input?.ToList() ?? new List<SlotInput>();
        if (list.Count == 0)
            throw Bad("Cần ít nhất 1 khung giờ.");
        if (list.Count > MaxSlots)
            throw Bad($"Tối đa {MaxSlots} khung giờ cho mỗi điểm hiến.");

        var parsed = new List<(TimeOnly Start, TimeOnly End, int Cap)>();
        for (var i = 0; i < list.Count; i++)
        {
            var slot = list[i];
            if (!TimeOnly.TryParseExact(slot.StartTime?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var s) ||
                !TimeOnly.TryParseExact(slot.EndTime?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var e))
                throw Bad($"Khung giờ #{i + 1}: giờ phải đúng định dạng HH:mm.");
            if (e <= s)
                throw Bad($"Khung giờ #{i + 1}: giờ kết thúc phải sau giờ bắt đầu.");
            if (slot.Capacity < 1 || slot.Capacity > 500)
                throw Bad($"Khung giờ #{i + 1}: sức chứa phải từ 1 đến 500 người.");
            parsed.Add((s, e, slot.Capacity));
        }

        var ordered = parsed.OrderBy(x => x.Start).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start < ordered[i - 1].End)
                throw Bad($"Khung giờ {SiteMapper.Fmt(ordered[i].Start)}–{SiteMapper.Fmt(ordered[i].End)} bị chồng lấn với khung giờ trước.");
        }

        return ordered
            .Select(x => new SlotDto { StartTime = SiteMapper.Fmt(x.Start), EndTime = SiteMapper.Fmt(x.End), Capacity = x.Cap })
            .ToList();
    }

    private static HttpException Bad(string message) => new(StatusCodes.Status400BadRequest, message);
}
