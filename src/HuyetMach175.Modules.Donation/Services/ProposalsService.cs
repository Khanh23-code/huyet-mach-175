using System.Text.Json;
using System.Text.RegularExpressions;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Proposals;
using HuyetMach175.Modules.Donation.DTOs.Sites;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class ProposalsService : IProposalsService
{
    private static readonly Regex PhoneRegex = new(@"^0\d{9}$", RegexOptions.Compiled);
    private static readonly Regex PhoneSeparators = new(@"[\s.\-]", RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;

    public ProposalsService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ------------------------------------------------------------ Tạo đề xuất
    public async Task<ProposalDto> CreateAsync(CreateProposalRequest request, int userId)
    {
        if (!Enum.TryParse<ProposalType>(request.ProposalType, true, out var type) || !Enum.IsDefined(type))
            throw Bad("Loại đề xuất không hợp lệ (NEW_SITE hoặc UPDATE_SCHEDULE).");

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người dùng không hợp lệ hoặc đã bị vô hiệu hóa.");

        var slots = SlotValidator.ValidateAndNormalize(request.Slots);

        var proposal = new SiteProposal
        {
            ProposalType = type,
            Status = ProposalStatus.PENDING,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            SlotsJson = JsonSerializer.Serialize(slots)
        };

        if (type == ProposalType.NEW_SITE)
        {
            var name = request.SiteName?.Trim();
            if (string.IsNullOrEmpty(name)) throw Bad("Vui lòng nhập tên điểm hiến.");

            if (!Enum.TryParse<SiteType>(request.SiteType, true, out var siteType) || !Enum.IsDefined(siteType))
                throw Bad("Loại điểm hiến không hợp lệ (FIXED hoặc MOBILE).");

            var address = request.Address?.Trim();
            if (string.IsNullOrEmpty(address)) throw Bad("Vui lòng nhập địa chỉ đầy đủ.");

            var phone = NormalizePhone(request.PhoneNumber);
            if (!PhoneRegex.IsMatch(phone)) throw Bad("Số điện thoại gồm 10 số, bắt đầu bằng 0.");

            var lower = name.ToLower();
            if (await _context.DonationSites.AnyAsync(s => s.SiteName.ToLower() == lower))
                throw Bad($"Đã tồn tại điểm hiến tên '{name}'.");
            if (await _context.SiteProposals.AnyAsync(p =>
                    p.ProposalType == ProposalType.NEW_SITE && p.Status == ProposalStatus.PENDING && p.SiteName.ToLower() == lower))
                throw Bad($"Đã có đề xuất tạo điểm hiến '{name}' đang chờ duyệt.");

            proposal.SiteName = name;
            proposal.SiteType = siteType;
            proposal.Address = address;
            proposal.PhoneNumber = phone;
        }
        else
        {
            if (request.SiteId is null or <= 0) throw Bad("Vui lòng chọn điểm hiến cần đổi lịch.");

            var site = await _context.DonationSites
                .AsNoTracking()
                .Include(s => s.Slots)
                .FirstOrDefaultAsync(s => s.SiteId == request.SiteId);
            if (site == null) throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy điểm hiến.");
            if (!site.IsActive) throw Bad("Điểm hiến đã ngừng hoạt động, không thể đổi lịch.");

            var pending = await _context.SiteProposals
                .Where(p => p.SiteId == site.SiteId && p.Status == ProposalStatus.PENDING)
                .Select(p => p.ProposalCode)
                .FirstOrDefaultAsync();
            if (pending != null) throw Bad($"Điểm hiến này đang có đề xuất {pending} chờ duyệt.");

            var current = SiteMapper.ToSlotDtos(site.Slots);
            if (current.Count == slots.Count && current.Zip(slots).All(x =>
                    x.First.StartTime == x.Second.StartTime && x.First.EndTime == x.Second.EndTime && x.First.Capacity == x.Second.Capacity))
                throw Bad("Cấu hình đề xuất trùng với cấu hình hiện tại.");

            proposal.SiteId = site.SiteId;
            proposal.SiteName = site.SiteName;
            proposal.SiteType = site.SiteType;
            proposal.Address = site.Address;
            proposal.PhoneNumber = site.PhoneNumber;
        }

        proposal.ProposalCode = await NextCodeAsync();
        _context.SiteProposals.Add(proposal);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(proposal.ProposalId);
    }

    // ------------------------------------------------------------ Truy vấn
    public Task<PagedResult<ProposalDto>> GetMyProposalsAsync(ProposalFilterDto filter, int userId) =>
        QueryAsync(filter, userId);

    public Task<PagedResult<ProposalDto>> GetProposalsAsync(ProposalFilterDto filter) =>
        QueryAsync(filter, null);

    public async Task<ProposalDto> GetByIdAsync(int id)
    {
        if (id <= 0) throw Bad("ID đề xuất không hợp lệ.");

        var p = await BaseQuery().FirstOrDefaultAsync(x => x.ProposalId == id)
            ?? throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đề xuất với mã #{id}.");

        var current = new List<SlotDto>();
        if (p.ProposalType == ProposalType.UPDATE_SCHEDULE && p.SiteId.HasValue)
        {
            var slots = await _context.DonationSiteSlots
                .AsNoTracking()
                .Where(s => s.SiteId == p.SiteId.Value)
                .ToListAsync();
            current = SiteMapper.ToSlotDtos(slots);
        }

        return ToDto(p, current);
    }

    // ------------------------------------------------------------ Duyệt / từ chối
    public async Task<ProposalDto> ApproveAsync(int id, int userId)
    {
        var p = await LoadForReviewAsync(id, userId);
        var slots = JsonSerializer.Deserialize<List<SlotDto>>(p.SlotsJson) ?? new List<SlotDto>();

        await using var tx = await _context.Database.BeginTransactionAsync();

        if (p.ProposalType == ProposalType.NEW_SITE)
        {
            var site = new DonationSite
            {
                SiteName = p.SiteName,
                SiteType = p.SiteType,
                Address = p.Address,
                PhoneNumber = p.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Slots = slots.Select(s => SiteMapper.ToEntity(s)).ToList()
            };
            _context.DonationSites.Add(site);
            await _context.SaveChangesAsync();

            site.SiteCode = $"DH-{site.SiteId:D4}";
            p.SiteId = site.SiteId;
        }
        else
        {
            var site = await _context.DonationSites.FirstOrDefaultAsync(s => s.SiteId == p.SiteId)
                ?? throw Bad("Điểm hiến của đề xuất không còn tồn tại.");
            if (!site.IsActive) throw Bad("Điểm hiến đã ngừng hoạt động, không thể áp dụng đề xuất.");

            var old = await _context.DonationSiteSlots.Where(s => s.SiteId == site.SiteId).ToListAsync();
            _context.DonationSiteSlots.RemoveRange(old);
            _context.DonationSiteSlots.AddRange(slots.Select(s => SiteMapper.ToEntity(s, site.SiteId)));
            site.UpdatedAt = DateTime.UtcNow;
        }

        p.Status = ProposalStatus.APPROVED;
        p.ReviewedBy = userId;
        p.ReviewedAt = DateTime.UtcNow;
        p.RejectionReason = null;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetByIdAsync(p.ProposalId);
    }

    public async Task<ProposalDto> RejectAsync(int id, RejectProposalRequest request, int userId)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason)) throw Bad("Vui lòng nhập lý do từ chối.");

        var p = await LoadForReviewAsync(id, userId);
        p.Status = ProposalStatus.REJECTED;
        p.ReviewedBy = userId;
        p.ReviewedAt = DateTime.UtcNow;
        p.RejectionReason = reason;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(p.ProposalId);
    }

    // ------------------------------------------------------------ Helpers
    private async Task<SiteProposal> LoadForReviewAsync(int id, int userId)
    {
        if (id <= 0) throw Bad("ID đề xuất không hợp lệ.");

        var p = await _context.SiteProposals.FirstOrDefaultAsync(x => x.ProposalId == id)
            ?? throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đề xuất với mã #{id}.");

        if (p.Status != ProposalStatus.PENDING)
            throw Bad($"Đề xuất {p.ProposalCode} không ở trạng thái chờ duyệt (hiện tại: {p.Status}).");

        if (p.CreatedBy == userId)
            throw Bad("Không thể tự duyệt đề xuất do chính mình gửi.");

        var reviewer = await _context.Users.FindAsync(userId);
        if (reviewer == null || !reviewer.IsActive)
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người duyệt không hợp lệ hoặc đã bị vô hiệu hóa.");

        return p;
    }

    private async Task<PagedResult<ProposalDto>> QueryAsync(ProposalFilterDto filter, int? createdBy)
    {
        if (filter.PageNumber < 1) throw Bad("Số trang (PageNumber) phải lớn hơn hoặc bằng 1.");
        if (filter.PageSize < 1 || filter.PageSize > 100) throw Bad("Kích thước trang (PageSize) phải từ 1 đến 100.");

        ProposalStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (!Enum.TryParse<ProposalStatus>(filter.Status, true, out var st) || !Enum.IsDefined(st))
                throw Bad("Trạng thái không hợp lệ (PENDING, APPROVED, REJECTED).");
            status = st;
        }

        var query = BaseQuery();
        if (createdBy.HasValue) query = query.Where(p => p.CreatedBy == createdBy.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        var total = await query.CountAsync();

        // Hàng chờ duyệt: đề xuất gửi sớm nhất lên đầu. Còn lại: mới nhất lên đầu.
        var ordered = status == ProposalStatus.PENDING
            ? query.OrderBy(p => p.CreatedAt)
            : query.OrderByDescending(p => p.CreatedAt);

        var items = await ordered
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ProposalDto>
        {
            Items = items.Select(p => ToDto(p, new List<SlotDto>())).ToList(),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    private IQueryable<SiteProposal> BaseQuery() =>
        _context.SiteProposals
            .AsNoTracking()
            .Include(p => p.CreatedByUser).ThenInclude(u => u!.UserRoles).ThenInclude(ur => ur.Role)
            .Include(p => p.ReviewedByUser);

    private static ProposalDto ToDto(SiteProposal p, List<SlotDto> current)
    {
        var slots = JsonSerializer.Deserialize<List<SlotDto>>(p.SlotsJson) ?? new List<SlotDto>();
        return new ProposalDto
        {
            ProposalId = p.ProposalId,
            ProposalCode = p.ProposalCode,
            ProposalType = p.ProposalType.ToString(),
            Status = p.Status.ToString(),
            SiteId = p.SiteId,
            SiteName = p.SiteName,
            SiteType = p.SiteType.ToString(),
            Address = p.Address,
            PhoneNumber = p.PhoneNumber,
            Slots = slots,
            TotalCapacityPerDay = slots.Sum(s => s.Capacity),
            CurrentSlots = current,
            CreatedBy = p.CreatedBy,
            CreatedByName = p.CreatedByUser?.FullName ?? string.Empty,
            CreatedByRoleCode = p.CreatedByUser?.UserRoles
                .Select(ur => ur.Role != null ? ur.Role.RoleCode : null)
                .FirstOrDefault(r => r != null),
            CreatedAt = p.CreatedAt,
            ReviewedBy = p.ReviewedBy,
            ReviewedByName = p.ReviewedByUser?.FullName,
            ReviewedAt = p.ReviewedAt,
            RejectionReason = p.RejectionReason
        };
    }

    /// <summary>Mã dạng DX-yyMMdd-nnn (giờ Việt Nam), nnn tăng dần trong ngày.</summary>
    private async Task<string> NextCodeAsync()
    {
        var prefix = $"DX-{DateTime.UtcNow.AddHours(7):yyMMdd}-";
        var codes = await _context.SiteProposals
            .Where(p => p.ProposalCode.StartsWith(prefix))
            .Select(p => p.ProposalCode)
            .ToListAsync();

        var max = codes
            .Select(c => int.TryParse(c.AsSpan(prefix.Length), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{max + 1:D3}";
    }

    private static string NormalizePhone(string? phone) => PhoneSeparators.Replace(phone ?? string.Empty, string.Empty);

    private static HttpException Bad(string message) => new(StatusCodes.Status400BadRequest, message);
}
