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
            // W1 - Tiếp đón & Sàng lọc
            new Permission { PermissionCode = "W1_VIEW", PermissionName = "Xem Tiếp đón & Sàng lọc", ModuleCode = "W1", ModuleName = "Tiếp đón & Sàng lọc", ActionType = nameof(PermissionAction.VIEW), Description = "Quản lý hàng chờ, khám sàng lọc, phiên lấy máu" },
            new Permission { PermissionCode = "W1_EDIT", PermissionName = "Thao tác Tiếp đón & Sàng lọc", ModuleCode = "W1", ModuleName = "Tiếp đón & Sàng lọc", ActionType = nameof(PermissionAction.EDIT), Description = "Quản lý hàng chờ, khám sàng lọc, phiên lấy máu" },
            new Permission { PermissionCode = "W1_APPROVE", PermissionName = "Duyệt Tiếp đón & Sàng lọc", ModuleCode = "W1", ModuleName = "Tiếp đón & Sàng lọc", ActionType = nameof(PermissionAction.APPROVE), Description = "Quản lý hàng chờ, khám sàng lọc, phiên lấy máu" },
            new Permission { PermissionCode = "W1_CANCEL", PermissionName = "Hủy / Xóa Tiếp đón & Sàng lọc", ModuleCode = "W1", ModuleName = "Tiếp đón & Sàng lọc", ActionType = nameof(PermissionAction.CANCEL), Description = "Quản lý hàng chờ, khám sàng lọc, phiên lấy máu" },

            // W2 - Túi máu & Kho
            new Permission { PermissionCode = "W2_VIEW", PermissionName = "Xem Túi máu & Kho", ModuleCode = "W2", ModuleName = "Túi máu & Kho", ActionType = nameof(PermissionAction.VIEW), Description = "Khởi tạo, theo dõi vòng đời, kiểm kê và xử lý túi máu" },
            new Permission { PermissionCode = "W2_EDIT", PermissionName = "Thao tác Túi máu & Kho", ModuleCode = "W2", ModuleName = "Túi máu & Kho", ActionType = nameof(PermissionAction.EDIT), Description = "Khởi tạo, theo dõi vòng đời, kiểm kê và xử lý túi máu" },
            new Permission { PermissionCode = "W2_APPROVE", PermissionName = "Duyệt Túi máu & Kho", ModuleCode = "W2", ModuleName = "Túi máu & Kho", ActionType = nameof(PermissionAction.APPROVE), Description = "Khởi tạo, theo dõi vòng đời, kiểm kê và xử lý túi máu" },
            new Permission { PermissionCode = "W2_CANCEL", PermissionName = "Hủy / Xóa Túi máu & Kho", ModuleCode = "W2", ModuleName = "Túi máu & Kho", ActionType = nameof(PermissionAction.CANCEL), Description = "Khởi tạo, theo dõi vòng đời, kiểm kê và xử lý túi máu" },

            // W3 - Yêu cầu máu Lâm sàng
            new Permission { PermissionCode = "W3_VIEW", PermissionName = "Xem Yêu cầu máu Lâm sàng", ModuleCode = "W3", ModuleName = "Yêu cầu máu Lâm sàng", ActionType = nameof(PermissionAction.VIEW), Description = "Lập phiếu, theo dõi và nhận bàn giao túi máu" },
            new Permission { PermissionCode = "W3_EDIT", PermissionName = "Thao tác Yêu cầu máu Lâm sàng", ModuleCode = "W3", ModuleName = "Yêu cầu máu Lâm sàng", ActionType = nameof(PermissionAction.EDIT), Description = "Lập phiếu, theo dõi và nhận bàn giao túi máu" },
            new Permission { PermissionCode = "W3_APPROVE", PermissionName = "Duyệt Yêu cầu máu Lâm sàng", ModuleCode = "W3", ModuleName = "Yêu cầu máu Lâm sàng", ActionType = nameof(PermissionAction.APPROVE), Description = "Lập phiếu, theo dõi và nhận bàn giao túi máu" },
            new Permission { PermissionCode = "W3_CANCEL", PermissionName = "Hủy / Xóa Yêu cầu máu Lâm sàng", ModuleCode = "W3", ModuleName = "Yêu cầu máu Lâm sàng", ActionType = nameof(PermissionAction.CANCEL), Description = "Lập phiếu, theo dõi và nhận bàn giao túi máu" },

            // W4 - Điều phối Kho máu
            new Permission { PermissionCode = "W4_VIEW", PermissionName = "Xem Điều phối Kho máu", ModuleCode = "W4", ModuleName = "Điều phối Kho máu", ActionType = nameof(PermissionAction.VIEW), Description = "Cung cầu, hàng đợi yêu cầu và cấp phát máu" },
            new Permission { PermissionCode = "W4_EDIT", PermissionName = "Thao tác Điều phối Kho máu", ModuleCode = "W4", ModuleName = "Điều phối Kho máu", ActionType = nameof(PermissionAction.EDIT), Description = "Cung cầu, hàng đợi yêu cầu và cấp phát máu" },
            new Permission { PermissionCode = "W4_APPROVE", PermissionName = "Duyệt Điều phối Kho máu", ModuleCode = "W4", ModuleName = "Điều phối Kho máu", ActionType = nameof(PermissionAction.APPROVE), Description = "Cung cầu, hàng đợi yêu cầu và cấp phát máu" },
            new Permission { PermissionCode = "W4_CANCEL", PermissionName = "Hủy / Xóa Điều phối Kho máu", ModuleCode = "W4", ModuleName = "Điều phối Kho máu", ActionType = nameof(PermissionAction.CANCEL), Description = "Cung cầu, hàng đợi yêu cầu và cấp phát máu" },

            // W5 - Phê duyệt & Giám sát
            new Permission { PermissionCode = "W5_VIEW", PermissionName = "Xem Phê duyệt & Giám sát", ModuleCode = "W5", ModuleName = "Phê duyệt & Giám sát", ActionType = nameof(PermissionAction.VIEW), Description = "Phê duyệt hủy, báo cáo và giám sát vận hành" },
            new Permission { PermissionCode = "W5_EDIT", PermissionName = "Thao tác Phê duyệt & Giám sát", ModuleCode = "W5", ModuleName = "Phê duyệt & Giám sát", ActionType = nameof(PermissionAction.EDIT), Description = "Phê duyệt hủy, báo cáo và giám sát vận hành" },
            new Permission { PermissionCode = "W5_APPROVE", PermissionName = "Duyệt Phê duyệt & Giám sát", ModuleCode = "W5", ModuleName = "Phê duyệt & Giám sát", ActionType = nameof(PermissionAction.APPROVE), Description = "Phê duyệt hủy, báo cáo và giám sát vận hành" },
            new Permission { PermissionCode = "W5_CANCEL", PermissionName = "Hủy / Xóa Phê duyệt & Giám sát", ModuleCode = "W5", ModuleName = "Phê duyệt & Giám sát", ActionType = nameof(PermissionAction.CANCEL), Description = "Phê duyệt hủy, báo cáo và giám sát vận hành" },

            // W6 - Quản trị hệ thống
            new Permission { PermissionCode = "W6_VIEW", PermissionName = "Xem Quản trị hệ thống", ModuleCode = "W6", ModuleName = "Quản trị hệ thống", ActionType = nameof(PermissionAction.VIEW), Description = "Người dùng, phân quyền, danh mục, tích hợp và audit log" },
            new Permission { PermissionCode = "W6_EDIT", PermissionName = "Thao tác Quản trị hệ thống", ModuleCode = "W6", ModuleName = "Quản trị hệ thống", ActionType = nameof(PermissionAction.EDIT), Description = "Người dùng, phân quyền, danh mục, tích hợp và audit log" },
            new Permission { PermissionCode = "W6_APPROVE", PermissionName = "Duyệt Quản trị hệ thống", ModuleCode = "W6", ModuleName = "Quản trị hệ thống", ActionType = nameof(PermissionAction.APPROVE), Description = "Người dùng, phân quyền, danh mục, tích hợp và audit log" },
            new Permission { PermissionCode = "W6_CANCEL", PermissionName = "Hủy / Xóa Quản trị hệ thống", ModuleCode = "W6", ModuleName = "Quản trị hệ thống", ActionType = nameof(PermissionAction.CANCEL), Description = "Người dùng, phân quyền, danh mục, tích hợp và audit log" },
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

        var sysRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SYS");
        if (sysRole != null)
        {
            var allPermissions = await context.Permissions.ToListAsync();
            var assignedPermissionIds = await context.RolePermissions
                .Where(rp => rp.RoleId == sysRole.RoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            foreach (var perm in allPermissions)
            {
                if (!assignedPermissionIds.Contains(perm.PermissionId))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = sysRole.RoleId,
                        PermissionId = perm.PermissionId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();
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
