using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/inventory/storage-locations")]
[Authorize]
[Tags("Inventory.StorageLocations")]
public class StorageLocationsController : ControllerBase
{
    private readonly IStorageLocationsService _storageLocationsService;

    public StorageLocationsController(IStorageLocationsService storageLocationsService)
    {
        _storageLocationsService = storageLocationsService;
    }

    [HttpGet]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Lấy danh sách các vị trí tủ bảo quản lưu trữ máu")]
    [EndpointDescription(
        "Lấy danh sách các tủ lạnh và ngăn kệ lưu trữ trong kho máu kèm hỗ trợ tìm kiếm và phân trang.\n\n" +
        "**Bộ lọc & Phân trang:**\n" +
        "- `search`: Tìm kiếm theo tên tủ, mã vị trí, mã vạch.\n" +
        "- `availableOnly = true`: Chỉ lấy các vị trí đang hoạt động và chưa đầy.\n" +
        "- `isFull`: Lọc theo trạng thái đã đầy hay chưa.\n" +
        "- `isActive`: Lọc theo trạng thái đang sử dụng hay ngừng sử dụng.\n" +
        "- `pageNumber` / `page`: Trang cần xem (mặc định 1).\n" +
        "- `pageSize`: Số bản ghi trên mỗi trang (mặc định 20).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW** (các vai trò như BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách vị trí lưu trữ phân trang.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<StorageLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStorageLocations([FromQuery] StorageLocationFilterDto filter)
    {
        var result = await _storageLocationsService.GetStorageLocationsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Xem chi tiết một vị trí tủ/ngăn bảo quản máu")]
    [EndpointDescription(
        "Lấy thông tin chi tiết một ngăn kệ lưu trữ bao gồm tên tủ, số ngăn, nhiệt độ bảo quản và số túi đang lưu trữ.")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStorageLocationById(int id)
    {
        var result = await _storageLocationsService.GetStorageLocationByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Thêm mới vị trí tủ/ngăn bảo quản máu")]
    [EndpointDescription(
        "Tạo mới một vị trí ngăn kệ hoặc tủ lạnh lưu trữ trong kho máu.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Bắt buộc cung cấp tên tủ (`refrigeratorName`), số ngăn (`shelfNumber`), và nhiệt độ mục tiêu (`targetTemperature`).\n" +
        "- Tự động kiểm tra trùng lặp cặp tên tủ và số ngăn để tránh tạo trùng.\n" +
        "- Ghi nhật ký hệ thống `audit_logs` khi tạo thành công.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_EDIT** (BBNK, MGT).")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateStorageLocation([FromBody] CreateStorageLocationRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _storageLocationsService.CreateStorageLocationAsync(request, userId, ipAddress);
        return CreatedAtAction(nameof(GetStorageLocationById), new { id = result.LocationId }, result);
    }

    [HttpPut("{id:int}")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Cập nhật thông tin vị trí tủ/ngăn bảo quản")]
    [EndpointDescription(
        "Chỉnh sửa thông tin tên tủ, số ngăn, nhiệt độ bảo quản hoặc trạng thái đầy/hoạt động của vị trí lưu trữ.")]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStorageLocation(int id, [FromBody] UpdateStorageLocationRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _storageLocationsService.UpdateStorageLocationAsync(id, request, userId, ipAddress);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Xóa hoặc vô hiệu hóa vị trí tủ/ngăn bảo quản")]
    [EndpointDescription(
        "Xóa vị trí lưu trữ nếu chưa từng có túi máu, hoặc chuyển sang trạng thái ngưng hoạt động (IsActive = false) nếu từng có lịch sử lưu trữ.\n\n" +
        "**Quy tắc an toàn:** Không cho phép xóa nếu vị trí này hiện đang có túi máu đang bảo quản.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStorageLocation(int id)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _storageLocationsService.DeleteStorageLocationAsync(id, userId, ipAddress);
        return NoContent();
    }
}
