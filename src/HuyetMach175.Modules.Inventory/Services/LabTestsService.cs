using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.LabTests;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class LabTestsService : ILabTestsService
{
    private readonly ApplicationDbContext _context;

    public LabTestsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LabTestQueueItemDto>> GetLabTestQueueAsync(LabTestQueueFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.BloodBags
            .AsNoTracking()
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Include(b => b.Session)
                .ThenInclude(s => s!.Appointment)
                    .ThenInclude(a => a!.Donor)
            .Where(b => b.Status == BagStatus.READY_TESTING && !b.TestResults.Any())
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Barcode, $"%{kw}%") ||
                (b.Session != null && b.Session.Appointment != null && b.Session.Appointment.Donor != null &&
                    (EF.Functions.ILike(b.Session.Appointment.Donor.FullName, $"%{kw}%") ||
                     EF.Functions.ILike(b.Session.Appointment.Donor.CitizenId, $"%{kw}%"))));
        }

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(b => b.CollectedAt)
            .ThenBy(b => b.BagId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = entities.Select(b =>
        {
            var donor = b.Session?.Appointment?.Donor;
            var bloodGroup = $"{(b.BloodType == BloodType.AB ? "AB" : b.BloodType.ToString())}{(b.RhFactor == RhFactor.POSITIVE ? "+" : "-")}";

            return new LabTestQueueItemDto
            {
                BagId = b.BagId,
                Barcode = b.Barcode,
                DonorCode = donor?.CitizenId,
                DonorName = donor != null ? donor.FullName : "Tiếp nhận ngoài viện",
                ComponentTypeCode = b.ComponentType?.TypeCode.ToString() ?? string.Empty,
                ComponentTypeName = b.ComponentType?.TypeName ?? string.Empty,
                VolumeMl = b.VolumeMl,
                ExpectedBloodType = b.BloodType.ToString(),
                ExpectedRhFactor = b.RhFactor.ToString(),
                ExpectedBloodGroup = bloodGroup,
                StorageLocationId = b.LocationId,
                StorageLocationName = b.Location != null ? $"{b.Location.RefrigeratorName} - {b.Location.ShelfNumber}" : null,
                CollectedAt = b.CollectedAt,
                Status = b.Status.ToString()
            };
        }).ToList();

        return new PagedResult<LabTestQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<LabTestResultDto> RecordLabTestAsync(RecordLabTestRequest request, int userId, string? ipAddress)
    {
        var identifier = !string.IsNullOrWhiteSpace(request.Barcode)
            ? request.Barcode.Trim()
            : request.BagId?.Trim();

        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã định danh túi máu (barcode hoặc bagId) là bắt buộc.");
        }

        var antibodyResult = request.IrregularAntibody ?? request.MalariaResult ?? TestResult.NEGATIVE;

        int? numericId = int.TryParse(identifier, out var parsedId) ? parsedId : null;

        var bloodBag = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Include(b => b.Session)
                .ThenInclude(s => s!.Appointment)
                    .ThenInclude(a => a!.Donor)
            .FirstOrDefaultAsync(b => b.Barcode == identifier || (numericId.HasValue && b.BagId == numericId.Value));

        if (bloodBag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu với mã '{identifier}'.");
        }

        if (bloodBag.Status != BagStatus.READY_TESTING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu đang ở trạng thái '{bloodBag.Status}', không hợp lệ để xét nghiệm. Chỉ túi máu ở trạng thái 'READY_TESTING' mới được phép nhập kết quả.");
        }

        var alreadyTested = await _context.BloodTestResults.AnyAsync(tr => tr.BagId == bloodBag.BagId);
        if (alreadyTested)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu '{bloodBag.Barcode}' đã có kết quả xét nghiệm trước đó trong hệ thống.");
        }

        bool isPassed = request.HivResult == TestResult.NEGATIVE &&
                        request.HbvResult == TestResult.NEGATIVE &&
                        request.HcvResult == TestResult.NEGATIVE &&
                        request.SyphilisResult == TestResult.NEGATIVE &&
                        antibodyResult == TestResult.NEGATIVE;

        var conclusion = isPassed ? OverallConclusion.PASSED : OverallConclusion.FAILED;
        var now = DateTime.UtcNow;

        var oldBloodType = bloodBag.BloodType.ToString();
        var oldRhFactor = bloodBag.RhFactor.ToString();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var testResult = new BloodTestResult
        {
            BagId = bloodBag.BagId,
            TechnicianId = userId,
            HivResult = request.HivResult,
            HbvResult = request.HbvResult,
            HcvResult = request.HcvResult,
            SyphilisResult = request.SyphilisResult,
            IrregularAntibody = antibodyResult,
            ConfirmedBloodType = request.ConfirmedBloodType,
            ConfirmedRh = request.ConfirmedRhFactor,
            OverallConclusion = conclusion,
            TestedAt = now
        };

        _context.BloodTestResults.Add(testResult);

        string changeReason;
        string userMessage;

        if (isPassed)
        {
            bloodBag.Status = BagStatus.TESTED_PASSED;
            bloodBag.BloodType = request.ConfirmedBloodType;
            bloodBag.RhFactor = request.ConfirmedRhFactor;

            var donor = bloodBag.Session?.Appointment?.Donor;
            if (donor != null)
            {
                donor.BloodType = request.ConfirmedBloodType;
                donor.RhFactor = request.ConfirmedRhFactor;
            }

            var confirmedGroup = $"{(bloodBag.BloodType == BloodType.AB ? "AB" : bloodBag.BloodType.ToString())}{(bloodBag.RhFactor == RhFactor.POSITIVE ? "+" : "-")}";
            changeReason = $"Xét nghiệm vi sinh và định nhóm máu đạt chuẩn (PASSED). Nhóm máu khẳng định: {confirmedGroup}. Trạng thái túi chuyển sang TESTED_PASSED.";
            userMessage = $"Ghi nhận kết quả xét nghiệm thành công. Túi máu {bloodBag.Barcode} đạt chuẩn an toàn truyền máu (PASSED) và chuyển sang trạng thái TESTED_PASSED.";
        }
        else
        {
            var failedIndicators = new List<string>();
            if (request.HivResult == TestResult.POSITIVE) failedIndicators.Add("HIV");
            if (request.HbvResult == TestResult.POSITIVE) failedIndicators.Add("HBV");
            if (request.HcvResult == TestResult.POSITIVE) failedIndicators.Add("HCV");
            if (request.SyphilisResult == TestResult.POSITIVE) failedIndicators.Add("Giang mai");
            if (antibodyResult == TestResult.POSITIVE) failedIndicators.Add("Kháng thể bất thường");
            var failureDetails = string.Join(", ", failedIndicators);

            bloodBag.Status = BagStatus.LOCKED;
            bloodBag.DiscardReason = $"Xét nghiệm vi sinh dương tính: {failureDetails}";

            var donor = bloodBag.Session?.Appointment?.Donor;
            var donorInfo = donor != null ? $" Người hiến: {donor.FullName} (CCCD: {donor.CitizenId})." : string.Empty;
            changeReason = $"CẢNH BÁO BỆNH TRUYỀN NHIỄM: Túi máu {bloodBag.Barcode} có kết quả FAILED ({failureDetails}). Túi máu bị KHÓA (LOCKED) chờ hội đồng duyệt hủy.{donorInfo}";
            userMessage = $"CẢNH BÁO: Túi máu {bloodBag.Barcode} không đạt chuẩn an toàn do phát hiện dương tính với: {failureDetails}. Túi máu đã được KHÓA (LOCKED) niêm phong cách ly.";
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_bags",
            RecordId = bloodBag.BagId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = BagStatus.READY_TESTING.ToString(),
                bloodType = oldBloodType,
                rhFactor = oldRhFactor
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = bloodBag.Status.ToString(),
                bloodType = bloodBag.BloodType.ToString(),
                rhFactor = bloodBag.RhFactor.ToString(),
                overallConclusion = conclusion.ToString(),
                discardReason = bloodBag.DiscardReason
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = changeReason,
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var technician = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        var technicianName = technician?.FullName ?? $"KTV #{userId}";
        var finalConfirmedGroup = $"{(request.ConfirmedBloodType == BloodType.AB ? "AB" : request.ConfirmedBloodType.ToString())}{(request.ConfirmedRhFactor == RhFactor.POSITIVE ? "+" : "-")}";

        return new LabTestResultDto
        {
            TestId = testResult.TestId,
            BagId = bloodBag.BagId,
            Barcode = bloodBag.Barcode,
            HivResult = testResult.HivResult.ToString(),
            HbvResult = testResult.HbvResult.ToString(),
            HcvResult = testResult.HcvResult.ToString(),
            SyphilisResult = testResult.SyphilisResult.ToString(),
            IrregularAntibody = testResult.IrregularAntibody.ToString(),
            ConfirmedBloodType = testResult.ConfirmedBloodType.ToString(),
            ConfirmedRhFactor = testResult.ConfirmedRh.ToString(),
            ConfirmedBloodGroup = finalConfirmedGroup,
            OverallConclusion = testResult.OverallConclusion.ToString(),
            NewBagStatus = bloodBag.Status.ToString(),
            DiscardReason = bloodBag.DiscardReason,
            TechnicianId = userId,
            TechnicianName = technicianName,
            TestedAt = now,
            Message = userMessage
        };
    }
}