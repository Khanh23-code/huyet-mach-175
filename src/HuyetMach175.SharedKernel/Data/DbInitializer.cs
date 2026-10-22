using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Services;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        var defaultDepartments = new[]
        {
            new { DepartmentCode = "DEP_ADMIN", DepartmentName = "Trung tâm Công nghệ Thông tin" },
            new { DepartmentCode = "DEP_BBNK", DepartmentName = "Khoa Huyết học" },
            new { DepartmentCode = "DEP_DON", DepartmentName = "Trung tâm truyền máu" },
            new { DepartmentCode = "DEP_CLIN", DepartmentName = "Khoa Cấp cứu" },
            new { DepartmentCode = "DEP_MGT", DepartmentName = "Phòng Kế hoạch" }
        };

        foreach (var deptInfo in defaultDepartments)
        {
            var existingDept = await context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentCode == deptInfo.DepartmentCode);

            if (existingDept == null)
            {
                context.Departments.Add(new Department
                {
                    DepartmentCode = deptInfo.DepartmentCode,
                    DepartmentName = deptInfo.DepartmentName,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (existingDept.DepartmentName != deptInfo.DepartmentName)
            {
                existingDept.DepartmentName = deptInfo.DepartmentName;
            }
        }
        await context.SaveChangesAsync();

        var deptDict = await context.Departments
            .ToDictionaryAsync(d => d.DepartmentCode, d => d.DepartmentId);

        var defaultRoles = new List<Role>
        {
            new Role
            {
                RoleCode = "SYS",
                RoleName = "Quản trị hệ thống",
                Description = "Quyền cao nhất toàn bộ hệ thống",
                IsActive = true
            },
            new Role
            {
                RoleCode = "MGT",
                RoleName = "Ban Giám đốc / Quản lý",
                Description = "Xem báo cáo và phê duyệt chiến dịch",
                IsActive = true
            },
            new Role
            {
                RoleCode = "CLIN",
                RoleName = "Bác sĩ / Nhân viên Lâm sàng",
                Description = "Tạo y lệnh và yêu cầu truyền máu",
                IsActive = true
            },
            new Role
            {
                RoleCode = "BBNK",
                RoleName = "Nhân viên Ngân hàng máu",
                Description = "Quản lý kho máu, xét nghiệm và cấp phát",
                IsActive = true
            },
            new Role
            {
                RoleCode = "DON",
                RoleName = "Nhân viên Tiếp nhận & Lấy máu",
                Description = "Tiếp nhận tình nguyện viên, hướng dẫn và thực hiện lấy máu",
                IsActive = true
            }
        };

        foreach (var role in defaultRoles)
        {
            var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == role.RoleCode);
            if (existingRole == null)
            {
                context.Roles.Add(role);
            }
            else
            {
                existingRole.RoleName = role.RoleName;
                existingRole.Description = role.Description;
            }
        }
        await context.SaveChangesAsync();

        var defaultPermissions = new List<Permission>
        {
            // CAMPAIGN
            new Permission { PermissionCode = "CAMPAIGN_VIEW", PermissionName = "Xem Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.VIEW, Description = "Quản lý Đợt hiến máu" },
            new Permission { PermissionCode = "CAMPAIGN_EDIT", PermissionName = "Tạo đơn Đề xuất & Sửa Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.EDIT, Description = "Quản lý Đợt hiến máu" },
            new Permission { PermissionCode = "CAMPAIGN_APPROVE", PermissionName = "Duyệt đơn Đề xuất Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.APPROVE, Description = "Quản lý Đợt hiến máu" },
            new Permission { PermissionCode = "CAMPAIGN_CANCEL", PermissionName = "Từ chối đơn Đề xuất Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.CANCEL, Description = "Quản lý Đợt hiến máu" },

            // DONORS
            new Permission { PermissionCode = "DONORS_VIEW", PermissionName = "Xem thông tin Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.VIEW, Description = "Quản lý thông tin người hiến máu" },
            new Permission { PermissionCode = "DONORS_EDIT", PermissionName = "Tạo mới / Sửa thông tin Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.EDIT, Description = "Quản lý thông tin người hiến máu" },
            // new Permission { PermissionCode = "DONORS_APPROVE", PermissionName = "Duyệt Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.APPROVE, Description = "Quản lý thông tin người hiến máu" },
            // new Permission { PermissionCode = "DONORS_CANCEL", PermissionName = "Hủy / Xóa Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.CANCEL, Description = "Quản lý thông tin người hiến máu" },

            // APPOINTMENTS
            new Permission { PermissionCode = "APPOINTMENTS_VIEW", PermissionName = "Xem danh sách Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.VIEW, Description = "Quản lý lịch hẹn" },
            new Permission { PermissionCode = "APPOINTMENTS_EDIT", PermissionName = "Tạo mới / Check-in cho Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.EDIT, Description = "Quản lý lịch hẹn" },
            // new Permission { PermissionCode = "APPOINTMENTS_APPROVE", PermissionName = "Duyệt Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.APPROVE, Description = "Quản lý lịch hẹn" },
            new Permission { PermissionCode = "APPOINTMENTS_CANCEL", PermissionName = "Hủy Lịch hẹn khi người hiến máu không đến", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.CANCEL, Description = "Quản lý lịch hẹn" },

            // SURVEY
            new Permission { PermissionCode = "SURVEY_VIEW", PermissionName = "Xem Bảng hỏi Online của Người hiến máu", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.VIEW, Description = "Quản lý khảo sát" },
            new Permission { PermissionCode = "SURVEY_EDIT", PermissionName = "Duyệt kết quả Bảng hỏi", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.EDIT, Description = "Quản lý khảo sát" },
            // new Permission { PermissionCode = "SURVEY_APPROVE", PermissionName = "Duyệt Khảo sát", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.APPROVE, Description = "Quản lý khảo sát" },
            // new Permission { PermissionCode = "SURVEY_CANCEL", PermissionName = "Hủy / Xóa Khảo sát", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.CANCEL, Description = "Quản lý khảo sát" },

            // SCREENING
            new Permission { PermissionCode = "SCREENING_VIEW", PermissionName = "Xem Danh sách chờ Khám lâm sàng / Phiếu khám chi tiết", ModuleCode = "SCREENING", ModuleName = "Khám lâm sàng", ActionType = PermissionAction.VIEW, Description = "Quản lý khám lâm sàng và sàng lọc người hiến máu" },
            new Permission { PermissionCode = "SCREENING_EDIT", PermissionName = "Ghi nhận, gửi kết quả Khám lâm sàng", ModuleCode = "SCREENING", ModuleName = "Khám lâm sàng", ActionType = PermissionAction.EDIT, Description = "Quản lý khám lâm sàng và sàng lọc người hiến máu" },
        };

        foreach (var p in defaultPermissions)
        {
            var existingPermission = await context.Permissions
                .FirstOrDefaultAsync(item => item.PermissionCode == p.PermissionCode);

            if (existingPermission == null)
            {
                context.Permissions.Add(p);
            }
            else
            {
                existingPermission.PermissionName = p.PermissionName;
                existingPermission.ModuleCode = p.ModuleCode;
                existingPermission.ModuleName = p.ModuleName;
                existingPermission.ActionType = p.ActionType;
                existingPermission.Description = p.Description;
            }
        }
        await context.SaveChangesAsync();

        var defaultPermissionCodes = defaultPermissions.Select(p => p.PermissionCode).ToHashSet();
        var obsoletePermissions = await context.Permissions
            .Where(p => !defaultPermissionCodes.Contains(p.PermissionCode))
            .ToListAsync();

        if (obsoletePermissions.Count > 0)
        {
            context.Permissions.RemoveRange(obsoletePermissions);
            await context.SaveChangesAsync();
        }

        var allDbPermissions = await context.Permissions.ToListAsync();
        var permMap = allDbPermissions.ToDictionary(p => p.PermissionCode, p => p.PermissionId);

        // Clean up any business/clinical permissions mistakenly assigned to SYS (SYS manages system only, not clinical operations)
        var sysRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SYS");
        if (sysRole != null)
        {
            var sysRolePerms = await context.RolePermissions
                .Where(rp => rp.RoleId == sysRole.RoleId)
                .ToListAsync();

            if (sysRolePerms.Count > 0)
            {
                context.RolePermissions.RemoveRange(sysRolePerms);
                await context.SaveChangesAsync();
            }
        }

        var defaultRolePermissions = new Dictionary<string, string[]>
        {
            ["MGT"] = new[]
            {
                "CAMPAIGN_VIEW", "CAMPAIGN_EDIT", "CAMPAIGN_APPROVE", "CAMPAIGN_CANCEL",
                "DONORS_VIEW", "APPOINTMENTS_VIEW", "SURVEY_VIEW", "SCREENING_VIEW"
            },
            ["CLIN"] = new[]
            {
                "SCREENING_VIEW", "SCREENING_EDIT",
                "SURVEY_VIEW", "SURVEY_EDIT",
                "DONORS_VIEW", "DONORS_EDIT",
                "APPOINTMENTS_VIEW"
            },
            ["DON"] = new[]
            {
                "APPOINTMENTS_VIEW", "APPOINTMENTS_EDIT", "APPOINTMENTS_CANCEL",
                "DONORS_VIEW", "DONORS_EDIT",
                "SURVEY_VIEW", "CAMPAIGN_VIEW"
            },
            ["BBNK"] = new[]
            {
                "CAMPAIGN_VIEW", "CAMPAIGN_EDIT", "DONORS_VIEW", "SCREENING_VIEW"
            }
        };

        foreach (var (roleCode, permCodes) in defaultRolePermissions)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == roleCode);
            if (role == null) continue;

            var existingPermIds = await context.RolePermissions
                .Where(rp => rp.RoleId == role.RoleId)
                .Select(rp => rp.PermissionId)
                .ToHashSetAsync();

            bool hasChanges = false;
            foreach (var code in permCodes)
            {
                if (permMap.TryGetValue(code, out int permId) && !existingPermIds.Contains(permId))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.RoleId,
                        PermissionId = permId,
                        CreatedAt = DateTime.UtcNow
                    });
                    hasChanges = true;
                }
            }
            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }

        var defaultPasswordHash = passwordHasher.HashPassword("password123");
        var adminPasswordHash = passwordHasher.HashPassword("admin123");

        var usersToSeed = new[]
        {
            new
            {
                Username = "admin",
                StaffCode = "NV00001",
                PasswordHash = adminPasswordHash,
                FullName = "Quản trị viên Hệ thống",
                Email = "admin@huyetmach175.vn",
                PhoneNumber = "0900000001",
                DepartmentCode = "DEP_ADMIN",
                RoleCode = "SYS"
            },
            new
            {
                Username = "manager",
                StaffCode = "NV00002",
                PasswordHash = defaultPasswordHash,
                FullName = "Ban Giám Đốc",
                Email = "manager@huyetmach175.vn",
                PhoneNumber = "0900000002",
                DepartmentCode = "DEP_MGT",
                RoleCode = "MGT"
            },
            new
            {
                Username = "doctor",
                StaffCode = "NV00003",
                PasswordHash = defaultPasswordHash,
                FullName = "Bác sĩ Lâm Sàng",
                Email = "doctor@huyetmach175.vn",
                PhoneNumber = "0900000003",
                DepartmentCode = "DEP_CLIN",
                RoleCode = "CLIN"
            },
            new
            {
                Username = "bloodbank",
                StaffCode = "NV00004",
                PasswordHash = defaultPasswordHash,
                FullName = "Kỹ Thuật Viên Ngân Hàng Máu",
                Email = "bloodbank@huyetmach175.vn",
                PhoneNumber = "0900000004",
                DepartmentCode = "DEP_BBNK",
                RoleCode = "BBNK"
            },
            new
            {
                Username = "donorstaff",
                StaffCode = "NV00005",
                PasswordHash = defaultPasswordHash,
                FullName = "Nhân Viên Tiếp Nhận & Lấy Máu",
                Email = "donorstaff@huyetmach175.vn",
                PhoneNumber = "0900000005",
                DepartmentCode = "DEP_DON",
                RoleCode = "DON"
            }
        };

        foreach (var u in usersToSeed)
        {
            var deptId = deptDict[u.DepartmentCode];
            var user = await context.Users.FirstOrDefaultAsync(item => item.Username == u.Username);
            if (user == null)
            {
                user = new User
                {
                    DepartmentId = deptId,
                    Username = u.Username,
                    StaffCode = u.StaffCode,
                    PasswordHash = u.PasswordHash,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
            else
            {
                user.DepartmentId = deptId;
                if (string.IsNullOrEmpty(user.StaffCode))
                {
                    user.StaffCode = u.StaffCode;
                }
                await context.SaveChangesAsync();
            }

            var targetRole = await context.Roles.FirstAsync(r => r.RoleCode == u.RoleCode);
            var userRoleExists = await context.UserRoles.AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == targetRole.RoleId);
            if (!userRoleExists)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = targetRole.RoleId,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }
    }
}