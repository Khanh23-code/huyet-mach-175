using HuyetMach175.Modules.Inventory.DTOs.BloodComponentTypes;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/inventory/blood-component-types")]
[Authorize]
[Tags("Inventory.BloodComponentTypes")]
public class BloodComponentTypesController : ControllerBase
{
    private readonly IBloodComponentTypesService _bloodComponentTypesService;

    public BloodComponentTypesController(IBloodComponentTypesService bloodComponentTypesService)
    {
        _bloodComponentTypesService = bloodComponentTypesService;
    }

    [HttpGet]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Lấy danh sách cấu hình các loại chế phẩm máu")]
    [EndpointDescription(
        "Trả về cấu hình quy tắc bảo quản, hạn dùng, tái nhập kho và hòa hợp miễn dịch của từng loại chế phẩm máu.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW**.")]
    [ProducesResponseType(typeof(List<BloodComponentTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _bloodComponentTypesService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Xem chi tiết cấu hình một loại chế phẩm máu")]
    [ProducesResponseType(typeof(BloodComponentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _bloodComponentTypesService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Cập nhật cấu hình nghiệp vụ của loại chế phẩm máu")]
    [EndpointDescription(
        "Cập nhật dải nhiệt độ bảo quản (`minTemp`, `maxTemp`), hạn sử dụng (`lifeDays`), quy tắc tái nhập kho (`allowRestock`, `maxOutsideMinutesForRestock`, `maxRestockCount`) và nhóm hòa hợp miễn dịch (`compatibilityRuleType`: `RBC`, `PLASMA`, `PLATELET`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_EDIT**.")]
    [ProducesResponseType(typeof(BloodComponentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBloodComponentTypeRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodComponentTypesService.UpdateAsync(id, request, userId, ipAddress);
        return Ok(result);
    }
}
