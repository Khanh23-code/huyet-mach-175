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
            var donor = session.Appointment?.Donor;
            if (donor != null)
            {
                donor.TotalDonations += 1;
                donor.LastDonationDate = DateOnly.FromDateTime(DateTime.Today);
            }

            var wbComponent = await _context.BloodComponentTypes
                .FirstOrDefaultAsync(ct => ct.TypeCode == ComponentCode.WB);

            if (wbComponent == null)
            {
                wbComponent = new BloodComponentType
                {
                    TypeCode = ComponentCode.WB,
                    TypeName = "Máu toàn phần",
                    LifeDays = 35,
                    StorageTemperature = "2°C - 6°C",
                    Description = "Máu toàn phần thu gom từ người hiến máu tình nguyện"
                };
                _context.BloodComponentTypes.Add(wbComponent);
                await _context.SaveChangesAsync();
            }

            bloodBagBarcode = $"BAG-{now:yyyyMMdd}-{session.SessionId:D5}";

            var bloodBag = new BloodBag
            {
                Barcode = bloodBagBarcode,
                SessionId = session.SessionId,
                ComponentTypeId = wbComponent.ComponentTypeId,
                BloodType = donor?.BloodType ?? BloodType.O,
                RhFactor = donor?.RhFactor ?? RhFactor.POSITIVE,
                VolumeMl = request.ActualVolumeMl!.Value,
                CollectedAt = now,
                ExpiredAt = now.AddDays(wbComponent.LifeDays),
                Status = BagStatus.NEWLY_COLLECTED,
                CreatedAt = now
            };
            _context.BloodBags.Add(bloodBag);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        string message = request.Status == SessionStatus.COMPLETED
            ? "Thu gom máu thành công. Túi máu mới đã được khởi tạo để chờ xét nghiệm và nhập kho."
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
}