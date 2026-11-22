using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Sessions;
using HuyetMach175.Modules.Donation.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class DonationSessionsService : IDonationSessionsService
{
    private readonly ApplicationDbContext _context;

    public DonationSessionsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SessionQueueItemDto>> GetSessionsAsync(SessionQueueFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var targetDate = filter.Date ?? DateOnly.FromDateTime(DateTime.Today);

        var query = _context.DonationSessions
            .AsNoTracking()
            .Include(s => s.Appointment)
                .ThenInclude(a => a!.Donor)
            .Include(s => s.Appointment)
                .ThenInclude(a => a!.PreScreeningSurvey)
                    .ThenInclude(sur => sur!.Doctor)
            .Where(s => s.Appointment != null && s.Appointment.AppointmentDate == targetDate);

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var statusStr = filter.Status.Trim().ToUpperInvariant();
            if (Enum.TryParse<SessionStatus>(statusStr, true, out var status))
            {
                query = query.Where(s => s.Status == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(s =>
                (s.Appointment != null && s.Appointment.Donor != null && (
                    EF.Functions.ILike(s.Appointment.Donor.FullName, $"%{kw}%") ||
                    EF.Functions.ILike(s.Appointment.Donor.CitizenId, $"%{kw}%") ||
                    EF.Functions.ILike(s.Appointment.Donor.PhoneNumber, $"%{kw}%")
                )) ||
                (s.Appointment != null && EF.Functions.ILike(s.Appointment.QrCodeToken, $"%{kw}%")));
        }

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(s => s.Appointment!.PreScreeningSurvey!.ScreenedAt ?? s.CreatedAt)
            .ThenBy(s => s.SessionId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = entities.Select(s => s.ToQueueItemDto()).ToList();

        return new PagedResult<SessionQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<SessionDetailDto> GetSessionByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID phiên lấy máu không hợp lệ.");
        }

        var session = await _context.DonationSessions
            .AsNoTracking()
            .Include(s => s.Appointment)
                .ThenInclude(a => a!.Donor)
            .Include(s => s.Appointment)
                .ThenInclude(a => a!.PreScreeningSurvey)
                    .ThenInclude(sur => sur!.Doctor)
            .Include(s => s.Phlebotomist)
            .Include(s => s.BloodBags)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiên lấy máu với ID {id}.");
        }

        return session.ToDetailDto();
    }

    public async Task<StartCollectionResponse> StartCollectionAsync(int sessionId, int phlebotomistId)
    {
        if (sessionId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID phiên lấy máu không hợp lệ.");
        }

        if (phlebotomistId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Không tìm thấy thông tin Kỹ thuật viên thực hiện lấy máu.");
        }

        var session = await _context.DonationSessions
            .Include(s => s.Appointment)
                .ThenInclude(a => a!.Donor)
            .Include(s => s.BloodBags)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiên lấy máu với ID {sessionId}.");
        }

        if (session.Status != SessionStatus.WAITING_COLLECTION)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Chỉ có thể bắt đầu lấy máu khi phiên đang ở trạng thái chờ lấy máu.");
        }

        // Idempotent: túi đã tạo từ lần bấm trước thì trả lại, không kiểm tra nhóm máu nữa.
        var existing = session.BloodBags.OrderBy(b => b.BagId).FirstOrDefault();
        if (existing != null)
        {
            return ToStartResponse(existing, false);
        }

        var (bloodType, rh) = RequireBloodGroup(session.Appointment?.Donor);

        var now = DateTime.UtcNow;
        var wb = await GetOrCreateWholeBloodTypeAsync();
        var bag = BuildBag(session, bloodType, rh, wb, BloodBagLabel.VolumeMl(session.TargetVolumeMl), now);

        session.PhlebotomistId = phlebotomistId;
        _context.BloodBags.Add(bag);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Hai request bắt đầu cùng lúc: request sau đụng unique barcode -> trả túi đã được tạo.
            _context.ChangeTracker.Clear();
            var created = await _context.BloodBags
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.SessionId == sessionId);
            if (created == null) throw;
            return ToStartResponse(created, false);
        }

        return ToStartResponse(bag, true);
    }

    public async Task<CollectionResponse> CompleteCollectionAsync(int sessionId, UpdateCollectionRequest request, int phlebotomistId)
    {
        if (sessionId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID phiên lấy máu không hợp lệ.");
        }

        if (phlebotomistId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Không tìm thấy thông tin Kỹ thuật viên thực hiện lấy máu.");
        }

        if (request.Status == SessionStatus.WAITING_COLLECTION)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Trạng thái đóng phiên phải là Hoàn tất (COMPLETED) hoặc Hủy/Sự cố (ABORTED).");
        }

        if (request.Status == SessionStatus.COMPLETED)
        {
            if (!request.ActualVolumeMl.HasValue || request.ActualVolumeMl.Value <= 0)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Bắt buộc phải nhập thể tích máu thực tế thu được (actualVolumeMl) khi hoàn tất phiên lấy máu.");
            }
        }
        else if (request.Status == SessionStatus.ABORTED)
        {
            if (string.IsNullOrWhiteSpace(request.CollectionIncident))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Bắt buộc phải ghi nhận lý do sự cố (collectionIncident) khi hủy hoặc dừng phiên lấy máu.");
            }
        }

        var session = await _context.DonationSessions
            .Include(s => s.Appointment)
                .ThenInclude(a => a!.Donor)
            .Include(s => s.BloodBags)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiên lấy máu với ID {sessionId}.");
        }

        if (session.Status == SessionStatus.COMPLETED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiên lấy máu này đã hoàn tất trước đó, không thể chỉnh sửa.");
        }

        if (session.Status == SessionStatus.ABORTED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiên lấy máu này đã bị hủy trước đó.");
        }

        var donor = session.Appointment?.Donor;
        var existingBag = session.BloodBags.OrderBy(b => b.BagId).FirstOrDefault();

        // Phiên chưa từng bấm Bắt đầu mà đóng COMPLETED: sẽ phải tạo túi mới -> kiểm tra nhóm máu TRƯỚC khi đổi dữ liệu.
        (BloodType BloodType, RhFactor Rh)? newBagBloodGroup = null;
        if (request.Status == SessionStatus.COMPLETED && existingBag == null)
        {
            newBagBloodGroup = RequireBloodGroup(donor);
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        session.PhlebotomistId = phlebotomistId;
        session.ActualVolumeMl = request.Status == SessionStatus.COMPLETED ? request.ActualVolumeMl : null;
        session.CollectionIncident = request.CollectionIncident;
        session.Status = request.Status;
        session.CompletedAt = now;

        string? bloodBagBarcode = null;

        if (request.Status == SessionStatus.COMPLETED)
        {
            if (donor != null)
            {
                donor.TotalDonations += 1;
                donor.LastDonationDate = DateOnly.FromDateTime(DateTime.Today);
            }

            if (existingBag != null)
            {
                // Túi đã sinh lúc bắt đầu: chốt thể tích thực tế.
                existingBag.VolumeMl = request.ActualVolumeMl!.Value;
                bloodBagBarcode = existingBag.Barcode;
            }
            else
            {
                var (bt, rhFactor) = newBagBloodGroup!.Value;
                var wb = await GetOrCreateWholeBloodTypeAsync();
                var newBag = BuildBag(session, bt, rhFactor, wb, request.ActualVolumeMl!.Value, now);
                _context.BloodBags.Add(newBag);
                bloodBagBarcode = newBag.Barcode;
            }
        }
        else
        {
            // ABORTED: túi đã sinh (nếu có) bị loại bỏ, không được kho tiếp nhận.
            var reason = request.CollectionIncident!.Trim();
            if (reason.Length > 255) reason = reason[..255];

            foreach (var bag in session.BloodBags)
            {
                bag.Status = BagStatus.DISCARDED;
                bag.DiscardReason = reason;
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        string message = request.Status == SessionStatus.COMPLETED
            ? "Thu gom máu thành công. Túi máu đã sẵn sàng để chờ xét nghiệm và nhập kho."
            : "Đã ghi nhận sự cố và hủy phiên lấy máu.";

        return new CollectionResponse
        {
            SessionId = session.SessionId,
            Status = session.Status.ToString(),
            ActualVolumeMl = session.ActualVolumeMl,
            CompletedAt = now,
            BloodBagId = bloodBagBarcode,
            Message = message
        };
    }

    /// <summary>Tem máu bắt buộc có nhóm máu thật: thiếu ABO hoặc Rh thì chặn, không đoán mặc định.</summary>
    private static (BloodType BloodType, RhFactor Rh) RequireBloodGroup(Donor? donor)
    {
        var bloodType = donor?.BloodType;
        var rh = donor?.RhFactor;

        if (bloodType is null || rh is null)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Người hiến chưa có nhóm máu (ABO/Rh). Vui lòng cập nhật nhóm máu ở bước khám sàng lọc trước khi lấy máu.");
        }

        return (bloodType.Value, rh.Value);
    }

    private async Task<BloodComponentType> GetOrCreateWholeBloodTypeAsync()
    {
        var wb = await _context.BloodComponentTypes
            .FirstOrDefaultAsync(ct => ct.TypeCode == ComponentCode.WB);

        if (wb != null) return wb;

        wb = new BloodComponentType
        {
            TypeCode = ComponentCode.WB,
            TypeName = "Máu toàn phần",
            LifeDays = 35,
            StorageTemperature = "2°C - 6°C",
            Description = "Máu toàn phần thu gom từ người hiến máu tình nguyện"
        };
        _context.BloodComponentTypes.Add(wb);
        await _context.SaveChangesAsync();
        return wb;
    }

    private static BloodBag BuildBag(DonationSession session, BloodType bloodType, RhFactor rh, BloodComponentType wb, int volumeMl, DateTime now) => new()
    {
        Barcode = BloodBagLabel.BuildBarcode(bloodType, rh, volumeMl, now, session.SessionId),
        SessionId = session.SessionId,
        ComponentTypeId = wb.ComponentTypeId,
        BloodType = bloodType,
        RhFactor = rh,
        VolumeMl = volumeMl,
        CollectedAt = now,
        ExpiredAt = now.AddDays(wb.LifeDays),
        Status = BagStatus.NEWLY_COLLECTED,
        CreatedAt = now
    };

    private static StartCollectionResponse ToStartResponse(BloodBag bag, bool isNew) => new()
    {
        SessionId = bag.SessionId ?? 0,
        BagId = bag.BagId,
        Barcode = bag.Barcode,
        BloodGroup = BloodBagLabel.GroupLabel(bag.BloodType, bag.RhFactor),
        VolumeMl = bag.VolumeMl,
        CollectedAt = bag.CollectedAt,
        Status = bag.Status.ToString(),
        IsNewBag = isNew,
        Message = isNew
            ? $"Đã tạo túi máu {bag.Barcode}. Vui lòng in và dán tem lên túi trước khi lấy máu."
            : $"Phiên này đã có túi máu {bag.Barcode}."
    };
}