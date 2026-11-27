using System.Text.Json;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class ClinicalSeeder
{
    public static async Task SeedClinicalDataAsync(ApplicationDbContext context)
    {
        if (await context.BloodRequests.AnyAsync(r => r.RequestCode.StartsWith("YC-SEED-")))
        {
            return;
        }

        var doctor = await context.Users.FirstOrDefaultAsync(u => u.Username == "doctor");
        var nurseClin = await context.Users.FirstOrDefaultAsync(u => u.Username == "nurse_clin");
        var bloodbank = await context.Users.FirstOrDefaultAsync(u => u.Username == "bloodbank");

        if (doctor == null || bloodbank == null)
        {
            return;
        }

        var nurseId = nurseClin?.UserId ?? doctor.UserId;
        var componentMap = await context.BloodComponentTypes.ToDictionaryAsync(c => c.TypeCode);
        if (!componentMap.TryGetValue(ComponentCode.PRBC, out var prbc) ||
            !componentMap.TryGetValue(ComponentCode.FFP, out var ffp) ||
            !componentMap.TryGetValue(ComponentCode.PLT, out var plt))
        {
            return;
        }

        var shelfB1 = await context.StorageLocations
            .Where(l => l.ShelfNumber == "Ngăn B1")
            .Select(l => l.LocationId)
            .FirstOrDefaultAsync();

        var shelfT1 = await context.StorageLocations
            .Where(l => l.ShelfNumber == "Kệ T1")
            .Select(l => l.LocationId)
            .FirstOrDefaultAsync();

        if (shelfB1 == Guid.Empty)
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. DRAFT: Phiếu nháp tại Khoa Cấp cứu (test sửa / gửi duyệt / xóa nháp)
        var reqDraft = new BloodRequest
        {
            RequestCode = "YC-SEED-001",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-001",
            PatientName = "Lê Thị Hoa",
            PatientBloodType = BloodType.A,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.ROUTINE,
            Diagnosis = "Thiếu máu mạn tính chuẩn bị phẫu thuật phiên",
            Status = RequestStatus.DRAFT,
            CreatedAt = now.AddHours(-2),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 1, AllocatedUnits = 0 }
            }
        };

        // 2. PENDING + EMERGENCY: Chờ Ngân hàng máu giữ chỗ túi máu khẩn cấp
        var reqPendingEmergency = new BloodRequest
        {
            RequestCode = "YC-SEED-002",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-002",
            PatientName = "Lê Hoàng Phúc",
            PatientBloodType = BloodType.B,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.EMERGENCY,
            Diagnosis = "Đa chấn thương mất máu cấp do tai nạn giao thông",
            Status = RequestStatus.PENDING,
            CreatedAt = now.AddMinutes(-25),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 2, AllocatedUnits = 0 }
            }
        };

        // 3. PENDING + URGENT (đa chế phẩm PRBC + FFP)
        var reqPendingUrgent = new BloodRequest
        {
            RequestCode = "YC-SEED-003",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-003",
            PatientName = "Đặng Minh Tuấn",
            PatientBloodType = BloodType.A,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.URGENT,
            Diagnosis = "Xuất huyết tiêu hóa trên kèm rối loạn đông máu",
            Status = RequestStatus.PENDING,
            CreatedAt = now.AddMinutes(-45),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 1, AllocatedUnits = 0 },
                new() { ComponentTypeId = ffp.ComponentTypeId, RequestedUnits = 1, AllocatedUnits = 0 }
            }
        };

        // 4. REJECTED: Phiếu bị từ chối có lý do
        var reqRejected = new BloodRequest
        {
            RequestCode = "YC-SEED-004",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-004",
            PatientName = "Vũ Thị Mai",
            PatientBloodType = BloodType.O,
            PatientRh = RhFactor.NEGATIVE,
            UrgencyLevel = UrgencyLevel.ROUTINE,
            Diagnosis = "Theo dõi thiếu máu nhẹ sau điều trị nội khoa",
            Status = RequestStatus.REJECTED,
            RejectionReason = "Chỉ số Hemoglobin hiện tại (115 g/L) chưa có chỉ định truyền khối hồng cầu thường quy, đề nghị hội chẩn lại.",
            CreatedAt = now.AddDays(-1),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 1, AllocatedUnits = 0 }
            }
        };

        // 5. READY (2 túi RESERVED đang nằm trong tủ Ngăn B1 -> sẵn sàng mở Pha 1 quét xuất kho tại /coordination/handover)
        var bagClinReserved1 = new BloodBag
        {
            Barcode = "BAG-SEED-CLIN-101",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-6),
            ExpiredAt = now.AddDays(36),
            Status = BagStatus.RESERVED,
            CreatedAt = now.AddDays(-6)
        };
        var bagClinReserved2 = new BloodBag
        {
            Barcode = "BAG-SEED-CLIN-102",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-6),
            ExpiredAt = now.AddDays(36),
            Status = BagStatus.RESERVED,
            CreatedAt = now.AddDays(-6)
        };

        var reqReadyForHandover = new BloodRequest
        {
            RequestCode = "YC-SEED-005",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-005",
            PatientName = "Trần Văn Bình",
            PatientBloodType = BloodType.O,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.URGENT,
            Diagnosis = "Phẫu thuật thay khớp háng có nguy cơ mất máu",
            Status = RequestStatus.READY,
            CreatedAt = now.AddHours(-3),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 2, AllocatedUnits = 2 }
            }
        };

        // 6. READY + Phiên bàn giao PENDING (Pha 1 đã quét xong, chờ Lâm sàng vào /clinical/handover xác nhận Pha 2)
        var bagClinPendingHandover = new BloodBag
        {
            Barcode = "BAG-SEED-CLIN-103",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-5),
            ExpiredAt = now.AddDays(37),
            Status = BagStatus.RESERVED,
            CreatedAt = now.AddDays(-5)
        };

        var reqPendingHandoverSession = new BloodRequest
        {
            RequestCode = "YC-SEED-006",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-006",
            PatientName = "Phạm Quốc Khánh",
            PatientBloodType = BloodType.A,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.URGENT,
            Diagnosis = "Thiếu máu nặng sau đợt hóa trị",
            Status = RequestStatus.READY,
            CreatedAt = now.AddHours(-2),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 1, AllocatedUnits = 1 }
            }
        };

        // 7. READY + Phiên bàn giao CONFIRMED: 1 túi đã TRANSFUSED + 1 túi đang ISSUED tại khoa (sẵn sàng quét Truyền máu hoặc Hoàn trả!)
        var bagClinTransfused = new BloodBag
        {
            Barcode = "BAG-SEED-CLIN-104",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-7),
            ExpiredAt = now.AddDays(35),
            Status = BagStatus.TRANSFUSED,
            CreatedAt = now.AddDays(-7)
        };

        var bagClinIssued = new BloodBag
        {
            Barcode = "BAG-SEED-CLIN-105",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-7),
            ExpiredAt = now.AddDays(35),
            Status = BagStatus.ISSUED,
            CreatedAt = now.AddDays(-7)
        };

        var reqAtWard = new BloodRequest
        {
            RequestCode = "YC-SEED-007",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-007",
            PatientName = "Nguyễn Thị Lan",
            PatientBloodType = BloodType.A,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.URGENT,
            Diagnosis = "Xuất huyết sau phẫu thuật sản phụ khoa",
            Status = RequestStatus.READY,
            CreatedAt = now.AddHours(-4),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 2, AllocatedUnits = 2 }
            }
        };

        // 8. READY + 3 kịch bản Hoàn trả máu (HT-SEED-001 chờ kiểm định PRBC, HT-SEED-002 chờ kiểm định PLT bắt buộc hủy, HT-SEED-003 đã tái nhập kho)
        var bagRetPendingPrbc = new BloodBag
        {
            Barcode = "BAG-SEED-RET-201",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-5),
            ExpiredAt = now.AddDays(37),
            Status = BagStatus.LOCKED,
            CreatedAt = now.AddDays(-5)
        };

        var bagRetPendingPlt = new BloodBag
        {
            Barcode = "BAG-SEED-RET-202",
            ComponentTypeId = plt.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 150,
            CollectedAt = now.AddDays(-2),
            ExpiredAt = now.AddDays(3),
            Status = BagStatus.LOCKED,
            CreatedAt = now.AddDays(-2)
        };

        var bagRetRestockedPrbc = new BloodBag
        {
            Barcode = "BAG-SEED-RET-203",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-8),
            ExpiredAt = now.AddDays(34),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-8)
        };

        var reqReturnCases = new BloodRequest
        {
            RequestCode = "YC-SEED-008",
            DoctorId = doctor.UserId,
            PatientCode = "BN-SEED-008",
            PatientName = "Hoàng Văn Nam",
            PatientBloodType = BloodType.O,
            PatientRh = RhFactor.POSITIVE,
            UrgencyLevel = UrgencyLevel.URGENT,
            Diagnosis = "Phẫu thuật cấp cứu ổ bụng",
            Status = RequestStatus.READY,
            CreatedAt = now.AddHours(-6),
            Items = new List<BloodRequestItem>
            {
                new() { ComponentTypeId = prbc.ComponentTypeId, RequestedUnits = 2, AllocatedUnits = 2 },
                new() { ComponentTypeId = plt.ComponentTypeId, RequestedUnits = 1, AllocatedUnits = 1 }
            }
        };

        context.BloodBags.AddRange(
            bagClinReserved1, bagClinReserved2,
            bagClinPendingHandover,
            bagClinTransfused, bagClinIssued,
            bagRetPendingPrbc, bagRetPendingPlt, bagRetRestockedPrbc);

        context.BloodRequests.AddRange(
            reqDraft,
            reqPendingEmergency,
            reqPendingUrgent,
            reqRejected,
            reqReadyForHandover,
            reqPendingHandoverSession,
            reqAtWard,
            reqReturnCases);

        await context.SaveChangesAsync();

        // Allocations cho YC-SEED-005 (2 RESERVED)
        var allocRes1 = new BloodAllocation
        {
            RequestId = reqReadyForHandover.RequestId,
            BagId = bagClinReserved1.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-2),
            Status = AllocationStatus.RESERVED
        };
        var allocRes2 = new BloodAllocation
        {
            RequestId = reqReadyForHandover.RequestId,
            BagId = bagClinReserved2.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-2),
            Status = AllocationStatus.RESERVED
        };

        // Allocation + Handover PENDING cho YC-SEED-006
        var allocPendingHo = new BloodAllocation
        {
            RequestId = reqPendingHandoverSession.RequestId,
            BagId = bagClinPendingHandover.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddMinutes(-50),
            Status = AllocationStatus.RESERVED
        };

        // Allocations cho YC-SEED-007 (1 COMPLETED + 1 ISSUED)
        var allocTransfused = new BloodAllocation
        {
            RequestId = reqAtWard.RequestId,
            BagId = bagClinTransfused.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-3),
            IssuedBy = bloodbank.UserId,
            ReceivedBy = nurseId,
            IssuedAt = now.AddHours(-2).AddMinutes(-30),
            Status = AllocationStatus.COMPLETED
        };
        var allocIssued = new BloodAllocation
        {
            RequestId = reqAtWard.RequestId,
            BagId = bagClinIssued.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-3),
            IssuedBy = bloodbank.UserId,
            ReceivedBy = nurseId,
            IssuedAt = now.AddHours(-2).AddMinutes(-30),
            Status = AllocationStatus.ISSUED
        };

        // Allocations cho YC-SEED-008 (3 túi hoàn trả)
        var allocRetPendingPrbc = new BloodAllocation
        {
            RequestId = reqReturnCases.RequestId,
            BagId = bagRetPendingPrbc.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-5),
            IssuedBy = bloodbank.UserId,
            ReceivedBy = doctor.UserId,
            IssuedAt = now.AddHours(-4),
            Status = AllocationStatus.ISSUED
        };
        var allocRetPendingPlt = new BloodAllocation
        {
            RequestId = reqReturnCases.RequestId,
            BagId = bagRetPendingPlt.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-5),
            IssuedBy = bloodbank.UserId,
            ReceivedBy = nurseId,
            IssuedAt = now.AddHours(-4),
            Status = AllocationStatus.ISSUED
        };
        var allocRetRestocked = new BloodAllocation
        {
            RequestId = reqReturnCases.RequestId,
            BagId = bagRetRestockedPrbc.BagId,
            ReservedBy = bloodbank.UserId,
            ReservedAt = now.AddHours(-5),
            IssuedBy = bloodbank.UserId,
            ReceivedBy = doctor.UserId,
            IssuedAt = now.AddHours(-4),
            Status = AllocationStatus.RETURNED
        };

        context.BloodAllocations.AddRange(
            allocRes1, allocRes2,
            allocPendingHo,
            allocTransfused, allocIssued,
            allocRetPendingPrbc, allocRetPendingPlt, allocRetRestocked);

        await context.SaveChangesAsync();

        // Phiên bàn giao PENDING (BG-SEED-001) & CONFIRMED (BG-SEED-002, BG-SEED-003)
        var sessionPending = new BloodHandoverSession
        {
            SessionCode = "BG-SEED-001",
            RequestId = reqPendingHandoverSession.RequestId,
            InitiatedBy = bloodbank.UserId,
            Status = HandoverSessionStatus.PENDING,
            CreatedAt = now.AddMinutes(-15),
            ExpiresAt = now.AddHours(6),
            Items = new List<BloodHandoverSessionItem>
            {
                new() { AllocationId = allocPendingHo.AllocationId, BagId = bagClinPendingHandover.BagId }
            }
        };

        var sessionAtWard = new BloodHandoverSession
        {
            SessionCode = "BG-SEED-002",
            RequestId = reqAtWard.RequestId,
            InitiatedBy = bloodbank.UserId,
            ConfirmedBy = nurseId,
            Status = HandoverSessionStatus.CONFIRMED,
            CreatedAt = now.AddHours(-2).AddMinutes(-40),
            ExpiresAt = now.AddHours(-2).AddMinutes(-10),
            ConfirmedAt = now.AddHours(-2).AddMinutes(-30),
            Items = new List<BloodHandoverSessionItem>
            {
                new() { AllocationId = allocTransfused.AllocationId, BagId = bagClinTransfused.BagId },
                new() { AllocationId = allocIssued.AllocationId, BagId = bagClinIssued.BagId }
            }
        };

        var sessionReturnCases = new BloodHandoverSession
        {
            SessionCode = "BG-SEED-003",
            RequestId = reqReturnCases.RequestId,
            InitiatedBy = bloodbank.UserId,
            ConfirmedBy = doctor.UserId,
            Status = HandoverSessionStatus.CONFIRMED,
            CreatedAt = now.AddHours(-4).AddMinutes(-15),
            ExpiresAt = now.AddHours(-3).AddMinutes(-45),
            ConfirmedAt = now.AddHours(-4),
            Items = new List<BloodHandoverSessionItem>
            {
                new() { AllocationId = allocRetPendingPrbc.AllocationId, BagId = bagRetPendingPrbc.BagId },
                new() { AllocationId = allocRetPendingPlt.AllocationId, BagId = bagRetPendingPlt.BagId },
                new() { AllocationId = allocRetRestocked.AllocationId, BagId = bagRetRestockedPrbc.BagId }
            }
        };

        context.BloodHandoverSessions.AddRange(sessionPending, sessionAtWard, sessionReturnCases);

        // 3 Phiếu hoàn trả mẫu với ReturnCode thân thiện
        var retPendingPrbc = new BloodReturn
        {
            ReturnCode = "HT-SEED-001",
            AllocationId = allocRetPendingPrbc.AllocationId,
            ReturnedBy = doctor.UserId,
            ReceivedBy = doctor.UserId,
            ReturnReason = "Bệnh nhân ổn định huyết động trong mổ, tạm ngưng truyền đơn vị thứ hai.",
            Qualified = false,
            FinalAction = ReturnFinalAction.PENDING_INSPECTION,
            ProcessedAt = now.AddMinutes(-35)
        };

        var retPendingPlt = new BloodReturn
        {
            ReturnCode = "HT-SEED-002",
            AllocationId = allocRetPendingPlt.AllocationId,
            ReturnedBy = nurseId,
            ReceivedBy = nurseId,
            ReturnReason = "Hoãn truyền khối tiểu cầu do bệnh nhân sốt phản ứng nhẹ trước truyền.",
            Qualified = false,
            FinalAction = ReturnFinalAction.PENDING_INSPECTION,
            ProcessedAt = now.AddMinutes(-20)
        };

        var retRestocked = new BloodReturn
        {
            ReturnCode = "HT-SEED-003",
            AllocationId = allocRetRestocked.AllocationId,
            ReturnedBy = doctor.UserId,
            ReceivedBy = bloodbank.UserId,
            ReturnReason = "Hoàn trả sớm ngay sau bàn giao do thay đổi chỉ định phẫu thuật.",
            Qualified = true,
            FinalAction = ReturnFinalAction.RESTOCK,
            ProcessedAt = now.AddHours(-3)
        };

        context.BloodReturns.AddRange(retPendingPrbc, retPendingPlt, retRestocked);
        await context.SaveChangesAsync();

        // Ghi AuditLog đồng bộ để gợi ý ngăn tủ cũ (previousLocationId) và cảnh báo checklist lâm sàng hoạt động chuẩn
        context.AuditLogs.AddRange(
            new AuditLog
            {
                TableName = "blood_allocations",
                RecordId = allocIssued.AllocationId,
                ActionType = ActionType.STATUS_CHANGE,
                OldState = JsonSerializer.Serialize(new { status = "RESERVED", bagStatus = "RESERVED", locationId = shelfB1 }),
                NewState = JsonSerializer.Serialize(new { status = "ISSUED", bagStatus = "ISSUED" }),
                PerformedBy = nurseId,
                ChangeReason = "Xác nhận tiếp nhận bàn giao túi máu tại khoa lâm sàng.",
                CreatedAt = now.AddHours(-2).AddMinutes(-30)
            },
            new AuditLog
            {
                TableName = "blood_allocations",
                RecordId = allocRetPendingPrbc.AllocationId,
                ActionType = ActionType.STATUS_CHANGE,
                OldState = JsonSerializer.Serialize(new { status = "RESERVED", bagStatus = "RESERVED", locationId = shelfB1 }),
                NewState = JsonSerializer.Serialize(new { status = "ISSUED", bagStatus = "ISSUED" }),
                PerformedBy = doctor.UserId,
                ChangeReason = "Xác nhận tiếp nhận bàn giao túi máu tại khoa lâm sàng.",
                CreatedAt = now.AddHours(-4)
            },
            new AuditLog
            {
                TableName = "blood_allocations",
                RecordId = allocRetPendingPlt.AllocationId,
                ActionType = ActionType.STATUS_CHANGE,
                OldState = JsonSerializer.Serialize(new { status = "RESERVED", bagStatus = "RESERVED", locationId = shelfT1 != Guid.Empty ? shelfT1 : shelfB1 }),
                NewState = JsonSerializer.Serialize(new { status = "ISSUED", bagStatus = "ISSUED" }),
                PerformedBy = nurseId,
                ChangeReason = "Xác nhận tiếp nhận bàn giao túi tiểu cầu tại khoa lâm sàng.",
                CreatedAt = now.AddHours(-4)
            },
            new AuditLog
            {
                TableName = "blood_returns",
                RecordId = retPendingPrbc.ReturnId,
                ActionType = ActionType.INSERT,
                OldState = JsonSerializer.Serialize(new { bagId = bagRetPendingPrbc.BagId, bagStatus = "ISSUED" }),
                NewState = JsonSerializer.Serialize(new
                {
                    returnId = retPendingPrbc.ReturnId,
                    returnCode = retPendingPrbc.ReturnCode,
                    allocationId = allocRetPendingPrbc.AllocationId,
                    bagId = bagRetPendingPrbc.BagId,
                    barcode = bagRetPendingPrbc.Barcode,
                    bagStatus = "LOCKED",
                    finalAction = "PENDING_INSPECTION",
                    isEligibleForRestockEvaluation = false,
                    failedChecklistFlags = new[] { "Thời gian ngoài tủ lạnh dưới 30 phút" }
                }),
                PerformedBy = doctor.UserId,
                ChangeReason = $"Lâm sàng lập phiếu hoàn trả {retPendingPrbc.ReturnCode} cho túi máu {bagRetPendingPrbc.Barcode}.",
                CreatedAt = now.AddMinutes(-35)
            },
            new AuditLog
            {
                TableName = "blood_returns",
                RecordId = retPendingPlt.ReturnId,
                ActionType = ActionType.INSERT,
                OldState = JsonSerializer.Serialize(new { bagId = bagRetPendingPlt.BagId, bagStatus = "ISSUED" }),
                NewState = JsonSerializer.Serialize(new
                {
                    returnId = retPendingPlt.ReturnId,
                    returnCode = retPendingPlt.ReturnCode,
                    allocationId = allocRetPendingPlt.AllocationId,
                    bagId = bagRetPendingPlt.BagId,
                    barcode = bagRetPendingPlt.Barcode,
                    bagStatus = "LOCKED",
                    finalAction = "PENDING_INSPECTION",
                    isEligibleForRestockEvaluation = false,
                    failedChecklistFlags = new[] { "Chế phẩm Khối tiểu cầu (PLT) không được phép tái nhập kho theo cấu hình." }
                }),
                PerformedBy = nurseId,
                ChangeReason = $"Lâm sàng lập phiếu hoàn trả {retPendingPlt.ReturnCode} cho túi máu {bagRetPendingPlt.Barcode}.",
                CreatedAt = now.AddMinutes(-20)
            });

        await context.SaveChangesAsync();
    }
}
