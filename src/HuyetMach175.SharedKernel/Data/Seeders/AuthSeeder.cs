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
            new()
            {
                RoleCode = "SYS",
                RoleName = "Quản trị hệ thống",
                Description = "Quyền cao nhất toàn bộ hệ thống",
                IsActive = true
            },
            new()
            {
                RoleCode = "MGT",
                RoleName = "Ban Giám đốc / Quản lý",
                Description = "Xem báo cáo, phê duyệt chiến dịch và biên bản tiêu hủy",
                IsActive = true
            },
            new()
            {
                RoleCode = "CLIN",
                RoleName = "Bác sĩ / Nhân viên Lâm sàng",
                Description = "Tạo y lệnh, tiếp nhận bàn giao, truyền máu tại giường và hoàn trả máu",
                IsActive = true
            },
            new()
            {
                RoleCode = "BBNK",
                RoleName = "Nhân viên Ngân hàng máu",
                Description = "Quản lý kho máu, xét nghiệm, điều chế, cấp phát và kiểm định hoàn trả",
                IsActive = true
            },
            new()
            {
                RoleCode = "DON",
                RoleName = "Nhân viên Tiếp nhận & Lấy máu",
                Description = "Tiếp nhận tình nguyện viên, hướng dẫn và thực hiện lấy máu",
                IsActive = true
            },
            new()
            {
                RoleCode = "NURSE",
                RoleName = "Điều dưỡng & Y tá",
                Description = "Vai trò mẫu đang tạm ngưng để kiểm thử bộ lọc trạng thái vai trò",
                IsActive = false
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
            new() { PermissionCode = "CAMPAIGN_VIEW", PermissionName = "Xem Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.VIEW, Description = "Quản lý Đợt hiến máu" },
            new() { PermissionCode = "CAMPAIGN_EDIT", PermissionName = "Tạo đơn Đề xuất & Sửa Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.EDIT, Description = "Quản lý Đợt hiến máu" },
            new() { PermissionCode = "CAMPAIGN_APPROVE", PermissionName = "Duyệt đơn Đề xuất Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.APPROVE, Description = "Quản lý Đợt hiến máu" },
            new() { PermissionCode = "CAMPAIGN_CANCEL", PermissionName = "Từ chối đơn Đề xuất Đợt hiến máu", ModuleCode = "CAMPAIGN", ModuleName = "Đợt hiến máu", ActionType = PermissionAction.CANCEL, Description = "Quản lý Đợt hiến máu" },

            new() { PermissionCode = "DONORS_VIEW", PermissionName = "Xem thông tin Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.VIEW, Description = "Quản lý thông tin người hiến máu" },
            new() { PermissionCode = "DONORS_EDIT", PermissionName = "Tạo mới / Sửa thông tin Người hiến máu", ModuleCode = "DONORS", ModuleName = "Người hiến máu", ActionType = PermissionAction.EDIT, Description = "Quản lý thông tin người hiến máu" },

            new() { PermissionCode = "APPOINTMENTS_VIEW", PermissionName = "Xem danh sách Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.VIEW, Description = "Quản lý lịch hẹn" },
            new() { PermissionCode = "APPOINTMENTS_EDIT", PermissionName = "Tạo mới / Check-in cho Lịch hẹn", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.EDIT, Description = "Quản lý lịch hẹn" },
            new() { PermissionCode = "APPOINTMENTS_CANCEL", PermissionName = "Hủy Lịch hẹn khi người hiến máu không đến", ModuleCode = "APPOINTMENTS", ModuleName = "Lịch hẹn", ActionType = PermissionAction.CANCEL, Description = "Quản lý lịch hẹn" },

            new() { PermissionCode = "SURVEY_VIEW", PermissionName = "Xem Bảng hỏi Online của Người hiến máu", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.VIEW, Description = "Quản lý khảo sát" },
            new() { PermissionCode = "SURVEY_EDIT", PermissionName = "Duyệt kết quả Bảng hỏi", ModuleCode = "SURVEY", ModuleName = "Khảo sát", ActionType = PermissionAction.EDIT, Description = "Quản lý khảo sát" },

            new() { PermissionCode = "SCREENING_VIEW", PermissionName = "Xem Danh sách chờ Khám lâm sàng / Phiếu khám chi tiết", ModuleCode = "SCREENING", ModuleName = "Khám lâm sàng", ActionType = PermissionAction.VIEW, Description = "Quản lý khám lâm sàng và sàng lọc người hiến máu" },
            new() { PermissionCode = "SCREENING_EDIT", PermissionName = "Ghi nhận, gửi kết quả Khám lâm sàng", ModuleCode = "SCREENING", ModuleName = "Khám lâm sàng", ActionType = PermissionAction.EDIT, Description = "Quản lý khám lâm sàng và sàng lọc người hiến máu" },

            new() { PermissionCode = "SESSION_VIEW", PermissionName = "Xem danh sách và chi tiết Phiên lấy máu", ModuleCode = "SESSIONS", ModuleName = "Thu gom máu", ActionType = PermissionAction.VIEW, Description = "Quản lý phiên lấy máu và thu gom" },
            new() { PermissionCode = "SESSION_EDIT", PermissionName = "Thực hiện thu gom và đóng Phiên lấy máu", ModuleCode = "SESSIONS", ModuleName = "Thu gom máu", ActionType = PermissionAction.EDIT, Description = "Quản lý phiên lấy máu và thu gom" },
            new() { PermissionCode = "INVENTORY_VIEW", PermissionName = "Xem danh sách Túi máu và Vị trí kho", ModuleCode = "INVENTORY", ModuleName = "Kho máu", ActionType = PermissionAction.VIEW, Description = "Quản lý tồn kho túi máu và vị trí lưu trữ" },
            new() { PermissionCode = "INVENTORY_EDIT", PermissionName = "Tiếp nhận túi máu về Viện", ModuleCode = "INVENTORY", ModuleName = "Kho máu", ActionType = PermissionAction.EDIT, Description = "Tiếp nhận và lưu trữ túi máu sau thu gom" },
            new() { PermissionCode = "LAB_VIEW", PermissionName = "Xem Hàng chờ và Kết quả xét nghiệm", ModuleCode = "LAB", ModuleName = "Xét nghiệm", ActionType = PermissionAction.VIEW, Description = "Quản lý hàng chờ và kết quả xét nghiệm túi máu" },
            new() { PermissionCode = "LAB_EDIT", PermissionName = "Ghi nhận kết quả xét nghiệm túi máu", ModuleCode = "LAB", ModuleName = "Xét nghiệm", ActionType = PermissionAction.EDIT, Description = "Nhập và xác nhận kết quả xét nghiệm vi sinh và nhóm máu" },
            new() { PermissionCode = "PROCESSING_VIEW", PermissionName = "Xem Hàng chờ điều chế máu", ModuleCode = "PROCESSING", ModuleName = "Điều chế máu", ActionType = PermissionAction.VIEW, Description = "Quản lý danh sách túi máu chờ điều chế tách thành phần" },
            new() { PermissionCode = "PROCESSING_EDIT", PermissionName = "Thực hiện phân tách chế phẩm máu", ModuleCode = "PROCESSING", ModuleName = "Điều chế máu", ActionType = PermissionAction.EDIT, Description = "Ghi nhận phân tách túi máu thành các chế phẩm con" },

            new() { PermissionCode = "USERS_VIEW", PermissionName = "Xem danh sách Người dùng", ModuleCode = "USERS", ModuleName = "Người dùng", ActionType = PermissionAction.VIEW, Description = "Quản lý tài khoản nhân viên" },
            new() { PermissionCode = "USERS_EDIT", PermissionName = "Tạo mới / Sửa / Khóa Người dùng", ModuleCode = "USERS", ModuleName = "Người dùng", ActionType = PermissionAction.EDIT, Description = "Quản lý tài khoản nhân viên" },

            new() { PermissionCode = "ROLES_VIEW", PermissionName = "Xem Vai trò và Mã quyền", ModuleCode = "ROLES", ModuleName = "Vai trò & Phân quyền", ActionType = PermissionAction.VIEW, Description = "Quản lý vai trò và phân quyền" },
            new() { PermissionCode = "ROLES_EDIT", PermissionName = "Tạo / Sửa Vai trò và Phân quyền", ModuleCode = "ROLES", ModuleName = "Vai trò & Phân quyền", ActionType = PermissionAction.EDIT, Description = "Quản lý vai trò và phân quyền" },

            new() { PermissionCode = "DISPOSAL_VIEW", PermissionName = "Xem danh sách và chi tiết Phiếu tiêu hủy", ModuleCode = "DISPOSAL", ModuleName = "Tiêu hủy máu", ActionType = PermissionAction.VIEW, Description = "Quản lý và tra cứu phiếu tiêu hủy túi máu" },
            new() { PermissionCode = "DISPOSAL_EDIT", PermissionName = "Lập và rút phiếu đề xuất tiêu hủy", ModuleCode = "DISPOSAL", ModuleName = "Tiêu hủy máu", ActionType = PermissionAction.EDIT, Description = "Lập phiếu đề xuất tiêu hủy và rút phiếu khi còn chờ duyệt" },
            new() { PermissionCode = "DISPOSAL_APPROVE", PermissionName = "Phê duyệt / Từ chối tiêu hủy túi máu", ModuleCode = "DISPOSAL", ModuleName = "Tiêu hủy máu", ActionType = PermissionAction.APPROVE, Description = "Phê duyệt hoặc từ chối đề xuất tiêu hủy túi máu" },

            new() { PermissionCode = "BLOOD_REQUEST_VIEW", PermissionName = "Xem danh sách và chi tiết Phiếu yêu cầu máu", ModuleCode = "BLOOD_REQUEST", ModuleName = "Điều phối máu", ActionType = PermissionAction.VIEW, Description = "Xem danh sách và chi tiết phiếu yêu cầu, cấp phát máu" },
            new() { PermissionCode = "BLOOD_REQUEST_EDIT", PermissionName = "Tạo, lưu nháp và tiếp nhận máu theo phiếu", ModuleCode = "BLOOD_REQUEST", ModuleName = "Điều phối máu", ActionType = PermissionAction.EDIT, Description = "Bác sĩ lâm sàng lập phiếu yêu cầu máu, xác nhận tiếp nhận và hoàn trả máu" },
            new() { PermissionCode = "BLOOD_REQUEST_APPROVE", PermissionName = "Duyệt, giữ chỗ và cấp phát túi máu", ModuleCode = "BLOOD_REQUEST", ModuleName = "Điều phối máu", ActionType = PermissionAction.APPROVE, Description = "Ngân hàng máu duyệt phiếu yêu cầu, giữ chỗ và xuất kho cấp phát máu" },
            new() { PermissionCode = "BLOOD_REQUEST_CANCEL", PermissionName = "Hủy hoặc từ chối phiếu yêu cầu máu", ModuleCode = "BLOOD_REQUEST", ModuleName = "Điều phối máu", ActionType = PermissionAction.CANCEL, Description = "Hủy phiếu nháp/chờ duyệt hoặc từ chối yêu cầu máu" },

            new() { PermissionCode = "BLOOD_RETURN_VIEW", PermissionName = "Xem danh sách và chi tiết Phiếu hoàn trả máu", ModuleCode = "BLOOD_RETURN", ModuleName = "Hoàn trả máu", ActionType = PermissionAction.VIEW, Description = "Xem danh sách và chi tiết phiếu hoàn trả túi máu" },
            new() { PermissionCode = "BLOOD_RETURN_EDIT", PermissionName = "Lập phiếu hoàn trả túi máu", ModuleCode = "BLOOD_RETURN", ModuleName = "Hoàn trả máu", ActionType = PermissionAction.EDIT, Description = "Lâm sàng lập phiếu hoàn trả túi máu chưa sử dụng" },
            new() { PermissionCode = "BLOOD_RETURN_APPROVE", PermissionName = "Kiểm định và xử lý túi máu hoàn trả", ModuleCode = "BLOOD_RETURN", ModuleName = "Hoàn trả máu", ActionType = PermissionAction.APPROVE, Description = "Ngân hàng máu kiểm định túi máu hoàn trả để tái nhập kho hoặc tiêu hủy" },
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
                "CAMPAIGN_VIEW", "CAMPAIGN_APPROVE", "CAMPAIGN_CANCEL",
                "DONORS_VIEW", "APPOINTMENTS_VIEW", "SURVEY_VIEW", "SCREENING_VIEW", "SESSION_VIEW", "INVENTORY_VIEW", "LAB_VIEW", "PROCESSING_VIEW",
                "DISPOSAL_VIEW", "DISPOSAL_APPROVE",
                "BLOOD_REQUEST_VIEW", "BLOOD_RETURN_VIEW"
            },
            ["CLIN"] = new[]
            {
                "SCREENING_VIEW", "SCREENING_EDIT",
                "SURVEY_VIEW", "SURVEY_EDIT",
                "DONORS_VIEW", "DONORS_EDIT",
                "APPOINTMENTS_VIEW", "SESSION_VIEW",
                "BLOOD_REQUEST_VIEW", "BLOOD_REQUEST_EDIT", "BLOOD_REQUEST_CANCEL",
                "BLOOD_RETURN_VIEW", "BLOOD_RETURN_EDIT"
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
                "DISPOSAL_VIEW", "DISPOSAL_EDIT",
                "BLOOD_REQUEST_VIEW", "BLOOD_REQUEST_APPROVE",
                "BLOOD_RETURN_VIEW", "BLOOD_RETURN_APPROVE"
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

            var hasChanges = false;
            foreach (var code in permCodes)
            {
                if (permMap.TryGetValue(code, out var permId) && !existingPermIds.Contains(permId))
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
                FullName = "BS. Nguyễn Minh Khoa",
                Email = "doctor@huyetmach175.vn",
                PhoneNumber = "0900000003",
                DepartmentCode = "DEP_CLIN",
                RoleCode = "CLIN"
            },
            new
            {
                Username = "nurse_clin",
                StaffCode = "NV00007",
                PasswordHash = defaultPasswordHash,
                FullName = "ĐD. Trần Thanh Thảo",
                Email = "nurse.clin@huyetmach175.vn",
                PhoneNumber = "0900000007",
                DepartmentCode = "DEP_CLIN",
                RoleCode = "CLIN"
            },
            new
            {
                Username = "bloodbank",
                StaffCode = "NV00004",
                PasswordHash = defaultPasswordHash,
                FullName = "KTV. Lê Quốc Bảo",
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
                FullName = "ĐD. Phạm Thu Trang",
                Email = "donorstaff@huyetmach175.vn",
                PhoneNumber = "0900000005",
                DepartmentCode = "DEP_DON",
                RoleCode = "DON"
            },
            new
            {
                Username = "disposal_tester",
                StaffCode = "NV00099",
                PasswordHash = defaultPasswordHash,
                FullName = "KTV. Vũ Hải Đăng",
                Email = "disposal_tester@huyetmach175.vn",
                PhoneNumber = "0900000099",
                DepartmentCode = "DEP_BBNK",
                RoleCode = "BBNK"
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
