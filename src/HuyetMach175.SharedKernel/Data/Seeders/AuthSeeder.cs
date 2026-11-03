using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Services;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class AuthSeeder
{
    public static async Task SeedAuthDataAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        var defaultDepartments = new[]
        {
            new { DepartmentCode = "DEP_ADMIN", DepartmentName = "Trung tÃ¢m CÃ´ng nghá»‡ ThÃ´ng tin" },
            new { DepartmentCode = "DEP_BBNK", DepartmentName = "Khoa Huyáº¿t há»c" },
            new { DepartmentCode = "DEP_DON", DepartmentName = "Trung tÃ¢m truyá»n mÃ¡u" },
            new { DepartmentCode = "DEP_CLIN", DepartmentName = "Khoa Cáº¥p cá»©u" },
            new { DepartmentCode = "DEP_MGT", DepartmentName = "PhÃ²ng Káº¿ hoáº¡ch" }
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
            new Permission { PermissionCode = "CAMPAIGN_VIEW", PermissionName = "Xem Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.VIEW, Description = "Quản lý Đợt hiến máu" },
            new Permission { PermissionCode = "CAMPAIGN_EDIT", PermissionName = "Tạo đơn Đề xuất & Sửa Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.EDIT, Description = "Quản lý Đợt hiến máu" },
            new Permission { PermissionCode = "CAMPAIGN_APPROVE", PermissionName = "Duyệt đơn Đề xuất Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.APPROVE, Description = "Quản lý Đợt hiến máu" },
            new Permission { PermissionCode = "CAMPAIGN_CANCEL", PermissionName = "Từ chối đơn Đề xuất Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.CANCEL, Description = "Quản lý Đợt hiến máu" },

            new Permission { PermissionCode = "DONORS_VIEW", PermissionName = "Xem thông tin Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.VIEW, Description = "Quản lý thông tin người hiến máu" },
            new Permission { PermissionCode = "DONORS_EDIT", PermissionName = "Tạo mới / Sửa thông tin Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.EDIT, Description = "Quản lý thông tin người hiến máu" },

            new Permission { PermissionCode = "APPOINTMENTS_VIEW", PermissionName = "Xem danh sách Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.VIEW, Description = "Quản lý lịch hẹn" },
            new Permission { PermissionCode = "APPOINTMENTS_EDIT", PermissionName = "Tạo mới / Check-in cho Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.EDIT, Description = "Quản lý lịch hẹn" },
            new Permission { PermissionCode = "APPOINTMENTS_CANCEL", PermissionName = "Hủy Lịch hẹn khi người hiến máu không đến", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.CANCEL, Description = "Quản lý lịch hẹn" },

            new Permission { PermissionCode = "SURVEY_VIEW", PermissionName = "Xem Bảng hỏi Online của Người hiến máu", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.VIEW, Description = "Quản lý khảo sát" },
            new Permission { PermissionCode = "SURVEY_EDIT", PermissionName = "Duyệt kết quả Bảng hỏi", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.EDIT, Description = "Quản lý khảo sát" },

            new Permission { PermissionCode = "SCREENING_VIEW", PermissionName = "Xem Danh sách chờ Khám lâm sàng / Phiếu khám chi tiết", ModuleCode = "SCREENING", ModuleName = "Khám lâm sàng", ActionType = PermissionAction.VIEW, Description = "Quản lý khám lâm sàng và sàng lọc người hiến máu" },
            new Permission { PermissionCode = "SCREENING_EDIT", PermissionName = "Ghi nhận, gửi kết quả Khám lâm sàng", ModuleCode = "SCREENING", ModuleName = "Khám lâm sàng", ActionType = PermissionAction.EDIT, Description = "Quản lý khám lâm sàng và sàng lọc người hiến máu" },

            new Permission { PermissionCode = "SESSION_VIEW", PermissionName = "Xem danh sách và chi tiết Phiên lấy máu", ModuleCode = "SESSIONS", ModuleName = "Thu gom máu", ActionType = PermissionAction.VIEW, Description = "Quản lý phiên lấy máu và thu gom" },
            new Permission { PermissionCode = "SESSION_EDIT", PermissionName = "Thực hiện thu gom và đóng Phiên lấy máu", ModuleCode = "SESSIONS", ModuleName = "Thu gom máu", ActionType = PermissionAction.EDIT, Description = "Quản lý phiên lấy máu và thu gom" },
            new Permission { PermissionCode = "INVENTORY_VIEW", PermissionName = "Xem danh sách Túi máu và Vị trí kho", ModuleCode = "INVENTORY", ModuleName = "Kho máu", ActionType = PermissionAction.VIEW, Description = "Quản lý tồn kho túi máu và vị trí lưu trữ" },
            new Permission { PermissionCode = "INVENTORY_EDIT", PermissionName = "Tiếp nhận túi máu về Viện", ModuleCode = "INVENTORY", ModuleName = "Kho máu", ActionType = PermissionAction.EDIT, Description = "Tiếp nhận và lưu trữ túi máu sau thu gom" },
            new Permission { PermissionCode = "LAB_VIEW", PermissionName = "Xem Hàng chờ và Kết quả xét nghiệm", ModuleCode = "LAB", ModuleName = "Xét nghiệm", ActionType = PermissionAction.VIEW, Description = "Quản lý hàng chờ và kết quả xét nghiệm túi máu" },
            new Permission { PermissionCode = "LAB_EDIT", PermissionName = "Ghi nhận kết quả xét nghiệm túi máu", ModuleCode = "LAB", ModuleName = "Xét nghiệm", ActionType = PermissionAction.EDIT, Description = "Nhập và xác nhận kết quả xét nghiệm vi sinh và nhóm máu" },
            new Permission { PermissionCode = "PROCESSING_VIEW", PermissionName = "Xem Hàng chờ điều chế máu", ModuleCode = "PROCESSING", ModuleName = "Điều chế máu", ActionType = PermissionAction.VIEW, Description = "Quản lý danh sách túi máu chờ điều chế tách thành phần" },
            new Permission { PermissionCode = "PROCESSING_EDIT", PermissionName = "Thực hiện phân tách chế phẩm máu", ModuleCode = "PROCESSING", ModuleName = "Điều chế máu", ActionType = PermissionAction.EDIT, Description = "Ghi nhận phân tách túi máu thành các chế phẩm con" },

            new Permission { PermissionCode = "USERS_VIEW", PermissionName = "Xem danh sách Người dùng", ModuleCode = "USERS", ModuleName = "Người dùng", ActionType = PermissionAction.VIEW, Description = "Quản lý tài khoản nhân viên" },
            new Permission { PermissionCode = "USERS_EDIT", PermissionName = "Tạo mới / Sửa / Khóa Người dùng", ModuleCode = "USERS", ModuleName = "Người dùng", ActionType = PermissionAction.EDIT, Description = "Quản lý tài khoản nhân viên" },

            new Permission { PermissionCode = "ROLES_VIEW", PermissionName = "Xem Vai trò và Mã quyền", ModuleCode = "ROLES", ModuleName = "Vai trò & Phân quyền", ActionType = PermissionAction.VIEW, Description = "Quản lý vai trò và phân quyền" },
            new Permission { PermissionCode = "ROLES_EDIT", PermissionName = "Tạo / Sửa Vai trò và Phân quyền", ModuleCode = "ROLES", ModuleName = "Vai trò & Phân quyền", ActionType = PermissionAction.EDIT, Description = "Quản lý vai trò và phân quyền" },

            new Permission { PermissionCode = "DISPOSAL_VIEW", PermissionName = "Xem danh sách và chi tiết Phiếu tiêu hủy", ModuleCode = "DISPOSAL", ModuleName = "Tiêu hủy máu", ActionType = PermissionAction.VIEW, Description = "Quản lý và tra cứu phiếu tiêu hủy túi máu" },
            new Permission { PermissionCode = "DISPOSAL_EDIT", PermissionName = "Lập và rút phiếu đề xuất tiêu hủy", ModuleCode = "DISPOSAL", ModuleName = "Tiêu hủy máu", ActionType = PermissionAction.EDIT, Description = "Lập phiếu đề xuất tiêu hủy và rút phiếu khi còn chờ duyệt" },
            new Permission { PermissionCode = "DISPOSAL_APPROVE", PermissionName = "Phê duyệt / Từ chối tiêu hủy túi máu", ModuleCode = "DISPOSAL", ModuleName = "Tiêu hủy máu", ActionType = PermissionAction.APPROVE, Description = "Phê duyệt hoặc từ chối đề xuất tiêu hủy túi máu" },
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
            ["SYS"] = new[]
            {
                "USERS_VIEW", "USERS_EDIT", "ROLES_VIEW", "ROLES_EDIT"
            },
            ["MGT"] = new[]
            {
                "CAMPAIGN_VIEW", "CAMPAIGN_EDIT", "CAMPAIGN_APPROVE", "CAMPAIGN_CANCEL",
                "DONORS_VIEW", "APPOINTMENTS_VIEW", "SURVEY_VIEW", "SCREENING_VIEW", "SESSION_VIEW", "INVENTORY_VIEW", "LAB_VIEW", "PROCESSING_VIEW",
                "DISPOSAL_VIEW", "DISPOSAL_APPROVE"
            },
            ["CLIN"] = new[]
            {
                "SCREENING_VIEW", "SCREENING_EDIT",
                "SURVEY_VIEW", "SURVEY_EDIT",
                "DONORS_VIEW", "DONORS_EDIT",
                "APPOINTMENTS_VIEW", "SESSION_VIEW"
            },
            ["DON"] = new[]
            {
                "APPOINTMENTS_VIEW", "APPOINTMENTS_EDIT", "APPOINTMENTS_CANCEL",
                "DONORS_VIEW", "DONORS_EDIT",
                "SURVEY_VIEW", "SURVEY_EDIT",
                "SCREENING_VIEW", "SCREENING_EDIT",
                "CAMPAIGN_VIEW", "SESSION_VIEW", "SESSION_EDIT"
            },
            ["BBNK"] = new[]
            {
                "CAMPAIGN_VIEW", "CAMPAIGN_EDIT", "DONORS_VIEW", "SCREENING_VIEW", "SESSION_VIEW", "INVENTORY_VIEW", "INVENTORY_EDIT", "LAB_VIEW", "LAB_EDIT", "PROCESSING_VIEW", "PROCESSING_EDIT",
                "DISPOSAL_VIEW", "DISPOSAL_EDIT"
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

        var mgtRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "MGT");
        if (mgtRole != null && permMap.TryGetValue("CAMPAIGN_EDIT", out var editPermId))
        {
            var stale = await context.RolePermissions
                .Where(rp => rp.RoleId == mgtRole.RoleId && rp.PermissionId == editPermId)
                .ToListAsync();
            if (stale.Count > 0)
            {
                context.RolePermissions.RemoveRange(stale);
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