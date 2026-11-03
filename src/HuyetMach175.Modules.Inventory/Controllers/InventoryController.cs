using HuyetMach175.Modules.Inventory.DTOs.Inventory;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
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
}