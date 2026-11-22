using HuyetMach175.Modules.Auth.DTOs.Permissions;

namespace HuyetMach175.Modules.Auth.Services;

public interface IPermissionsService
{
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request);
    Task<CreatePermissionsResultDto> CreatePermissionsAsync(CreatePermissionsRequest request);
}

