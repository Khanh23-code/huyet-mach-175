using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;

namespace HuyetMach175.Modules.Inventory.Mappings;

public static class BloodBagTransitionRules
{
    public static void ValidateTransition(BloodBag bag, BagStatus targetStatus, bool hasActiveDisposal, DateTime now)
    {
        // Chốt chặn 1: Xét nghiệm đạt (TESTED_PASSED) bắt buộc từ lab-tests
        if (targetStatus == BagStatus.TESTED_PASSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Không thể chuyển trạng thái sang TESTED_PASSED thủ công. Trạng thái này bắt buộc phải được kích hoạt tự động sau khi hoàn tất quy trình xét nghiệm an toàn truyền máu (/api/lab-tests).");
        }

        // Chốt chặn 2: Điều chế (PROCESSED) bắt buộc từ blood-processing
        if (targetStatus == BagStatus.PROCESSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Không thể chuyển trạng thái sang PROCESSED thủ công. Vui lòng thực hiện nghiệp vụ này tại màn hình Điều chế chế phẩm (/api/blood-processing/separate).");
        }

        // Chốt chặn 3: Không cho về lại NEWLY_COLLECTED
        if (targetStatus == BagStatus.NEWLY_COLLECTED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Không thể chuyển ngược trạng thái về NEWLY_COLLECTED (Mới thu gom).");
        }

        // Chốt chặn 4: Đã hủy không thể đổi
        if (bag.Status == BagStatus.DISCARDED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Túi máu đã bị tiêu hủy (DISCARDED), không thể thực hiện bất kỳ thao tác chuyển trạng thái nào.");
        }

        // Chốt chặn 5: Đã truyền không thể đổi
        if (bag.Status == BagStatus.TRANSFUSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Túi máu đã được truyền cho bệnh nhân (TRANSFUSED), không thể thay đổi trạng thái.");
        }

        // Chốt chặn 6: Trùng trạng thái
        if (bag.Status == targetStatus)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Túi máu hiện tại đã ở trạng thái '{BloodBagMappings.FormatLifecycleStatusLabel(bag.Status)}' ({bag.Status}).");
        }

        // Chốt chặn 7: Kiểm tra biên bản hủy đang chờ xử lý
        if (bag.Status == BagStatus.LOCKED && hasActiveDisposal)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Túi máu đang nằm trong một biên bản tiêu hủy đang chờ xử lý. Vui lòng hoàn tất hoặc rút biên bản tiêu hủy trước khi mở khóa.");
        }

        // Chốt chặn 8: Hết hạn sử dụng
        if (bag.ExpiredAt <= now)
        {
            if (targetStatus is BagStatus.AVAILABLE or BagStatus.RESERVED or BagStatus.ISSUED or BagStatus.TRANSFUSED)
            {
                throw new HttpException(StatusCodes.Status400BadRequest,
                    $"Túi máu đã hết hạn sử dụng vào lúc {bag.ExpiredAt:dd/MM/yyyy HH:mm}. Không thể chuyển sang trạng thái '{BloodBagMappings.FormatLifecycleStatusLabel(targetStatus)}'. Vui lòng tạm khóa (LOCKED) để xử lý tiêu hủy.");
            }
        }

        // Chốt chặn 9: State Machine logic
        bool isValid = (bag.Status, targetStatus) switch
        {
            (BagStatus.NEWLY_COLLECTED, BagStatus.READY_TESTING) => true,
            (BagStatus.TESTED_PASSED, BagStatus.AVAILABLE) => true,
            (BagStatus.AVAILABLE, BagStatus.RESERVED) => true,
            (BagStatus.RESERVED, BagStatus.ISSUED) => true,
            (BagStatus.RESERVED, BagStatus.AVAILABLE) => true,
            (BagStatus.ISSUED, BagStatus.TRANSFUSED) => true,
            (BagStatus.ISSUED, BagStatus.AVAILABLE) => true,
            (_, BagStatus.LOCKED) => true,
            (BagStatus.LOCKED, BagStatus.AVAILABLE) => true,
            (BagStatus.LOCKED, BagStatus.READY_TESTING) => true,
            (BagStatus.LOCKED, BagStatus.TESTED_PASSED) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Chuyển đổi trạng thái không hợp lệ từ '{BloodBagMappings.FormatLifecycleStatusLabel(bag.Status)}' sang '{BloodBagMappings.FormatLifecycleStatusLabel(targetStatus)}'.");
        }
    }
}