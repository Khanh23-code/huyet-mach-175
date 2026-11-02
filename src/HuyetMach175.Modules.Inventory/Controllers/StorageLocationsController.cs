using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
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
        "Lấy danh sách các tủ lạnh và ngăn kệ lưu trữ trong kho máu phục vụ việc lựa chọn vị trí khi tiếp nhận túi máu.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Mặc định (`availableOnly = true`): Chỉ trả về các vị trí đang hoạt động (`isActive = true`) và chưa đầy (`isFull = false`).\n" +
        "- Khi truyền `availableOnly = false`: Trả về toàn bộ danh sách vị trí lưu trữ trong hệ thống.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW** (các vai trò như BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách vị trí lưu trữ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_VIEW.")]
    [ProducesResponseType(typeof(List<StorageLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStorageLocations([FromQuery(Name = "availableOnly")] bool? availableOnly = true)
    {
        var result = await _storageLocationsService.GetStorageLocationsAsync(availableOnly);
        return Ok(result);
    }
}