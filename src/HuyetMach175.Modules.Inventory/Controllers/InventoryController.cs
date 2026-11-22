using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.Inventory;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
[Tags("Inventory.Management")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("summary")]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Báo cáo ma trận tổng hợp tồn kho máu (Inventory Dashboard Summary)")]
    [EndpointDescription(
        "Tổng hợp số lượng túi máu (totalBags) và tổng thể tích (totalVolumeMl) theo từng nhóm máu (ABO), yếu tố Rh và loại chế phẩm máu.\n\n" +
        "**Bộ lọc hỗ trợ:**\n" +
        "- `bloodType`: Lọc theo nhóm máu (A, B, AB, O).\n" +
        "- `rhFactor`: Lọc theo yếu tố Rh (POSITIVE, NEGATIVE, +, -).\n" +
        "- `componentType`: Lọc theo loại chế phẩm (WHOLE_BLOOD, RED_BLOOD_CELLS, PLASMA, PLATELETS, PRBC, FFP, PLT, WB).\n" +
        "- `status`: Lọc theo trạng thái túi máu (AVAILABLE, EXPIRED, DISPOSAL_PENDING, TESTING, PROCESSING...).\n" +
        "- `isExpiringSoon`: Lọc nhanh các túi cận hạn (còn hạn ≤ 3 ngày).\n" +
        "- `storageLocationId`: Lọc theo tủ/ngăn lưu trữ cụ thể.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW**.")]
    [ProducesResponseType(typeof(InventorySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventorySummary([FromQuery] InventorySummaryFilterDto filter)
    {
        var result = await _inventoryService.GetInventorySummaryAsync(filter);
        return Ok(result);
    }

    [HttpGet("expiring-soon")]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Tra cứu túi máu sắp hết hạn (Cảnh báo cận hạn FEFO)")]
    [EndpointDescription(
        "Lấy danh sách các túi máu sắp hết hạn theo ngưỡng ngày (`daysThreshold`, mặc định ≤ 3 ngày).\n\n" +
        "**Mục đích nghiệp vụ:** Hỗ trợ kỹ thuật viên và bác sĩ ưu tiên xuất kho theo nguyên tắc FEFO (First Expired, First Out) để tránh lãng phí máu.\n\n" +
        "**Bộ lọc & Phân trang:**\n" +
        "- `daysThreshold`: Số ngày cận hạn (mặc định 3 ngày).\n" +
        "- `bloodType`, `rhFactor`, `componentType`: Lọc theo nhóm máu và loại chế phẩm.\n" +
        "- `storageLocationId`: Lọc theo vị trí tủ/ngăn bảo quản.\n" +
        "- `pageNumber`, `pageSize`: Hỗ trợ phân trang kết quả.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW**.")]
    [ProducesResponseType(typeof(PagedResult<ExpiringSoonBagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetExpiringSoonBags([FromQuery] ExpiringSoonFilterDto filter)
    {
        var result = await _inventoryService.GetExpiringSoonBagsAsync(filter);
        return Ok(result);
    }

    [HttpPut("bags/{bagId:int}/storage-location")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Điều chuyển vị trí ngăn tủ/hộc lưu trữ túi máu")]
    [EndpointDescription(
        "Cập nhật vị trí ngăn tủ bảo quản mới cho một túi máu và lưu vết lịch sử di chuyển vào nhật ký hệ thống (audit_logs).\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Kiểm tra sự tồn tại của túi máu và tính khả dụng của vị trí đích (phải đang hoạt động và chưa đầy).\n" +
        "- Chặn điều chuyển nếu túi máu đã tiêu hủy, đã xuất kho hoặc đã hoàn tất truyền máu.\n" +
        "- Chặn điều chuyển nếu vị trí mới trùng với vị trí hiện tại của túi máu.\n" +
        "- Lưu vết chi tiết vị trí cũ, vị trí mới, người thực hiện và lý do/ghi chú điều chuyển.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_EDIT**.")]
    [ProducesResponseType(typeof(RelocateBloodBagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RelocateBloodBag(
        [FromRoute] int bagId,
        [FromBody] RelocateBloodBagRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _inventoryService.RelocateBloodBagAsync(bagId, request, userId, ipAddress);
        return Ok(result);
    }
}