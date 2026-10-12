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
            new Permission { PermissionCode = "W1_VIEW", PermissionName = "Xem tiếp nhận & sàng lọc", Description = "Tra cứu, xem danh sách và hồ sơ người hiến máu, phiên tiếp nhận" },
            new Permission { PermissionCode = "W1_EDIT", PermissionName = "Cập nhật tiếp nhận & sàng lọc", Description = "Tạo mới, chỉnh sửa thông tin người hiến máu và kết quả khám sàng lọc" },
            new Permission { PermissionCode = "W1_APPROVE", PermissionName = "Phê duyệt tiếp nhận hiến máu", Description = "Phê duyệt điều kiện lấy máu và hoàn thành phiên tiếp nhận" },
            new Permission { PermissionCode = "W1_CANCEL", PermissionName = "Hủy bỏ tiếp nhận hiến máu", Description = "Hủy phiên lấy máu hoặc đánh dấu hoãn/không đạt" },

            new Permission { PermissionCode = "W2_VIEW", PermissionName = "Xem xét nghiệm túi máu", Description = "Tra cứu, xem kết quả xét nghiệm sàng lọc túi máu và nhóm máu" },
            new Permission { PermissionCode = "W2_EDIT", PermissionName = "Nhập kết quả xét nghiệm", Description = "Nhập, chỉnh sửa thông số xét nghiệm huyết thanh và virus" },
            new Permission { PermissionCode = "W2_APPROVE", PermissionName = "Phê duyệt kết quả xét nghiệm", Description = "Xác nhận và duyệt kết luận an toàn túi máu" },
            new Permission { PermissionCode = "W2_CANCEL", PermissionName = "Hủy kết quả xét nghiệm", Description = "Hủy bỏ hoặc từ chối kết quả xét nghiệm nghi ngờ" },

            new Permission { PermissionCode = "W3_VIEW", PermissionName = "Xem kho & chế phẩm máu", Description = "Tra cứu danh mục kho, tồn kho các chế phẩm máu" },
            new Permission { PermissionCode = "W3_EDIT", PermissionName = "Cập nhật điều chế & kho máu", Description = "Thực hiện phân tách chế phẩm, chuyển kho, gắn nhãn" },
            new Permission { PermissionCode = "W3_APPROVE", PermissionName = "Phê duyệt nhập/xuất kho máu", Description = "Phê duyệt nhập kho thành phẩm hoặc chuyển đổi trạng thái túi máu" },
            new Permission { PermissionCode = "W3_CANCEL", PermissionName = "Hủy túi máu / Hủy điều chế", Description = "Hủy bỏ túi máu quá hạn, lỗi hoặc hủy mẻ điều chế" },

            new Permission { PermissionCode = "W4_VIEW", PermissionName = "Xem y lệnh & dự trù máu", Description = "Tra cứu danh sách y lệnh truyền máu và phiếu dự trù từ khoa lâm sàng" },
            new Permission { PermissionCode = "W4_EDIT", PermissionName = "Tạo/Sửa y lệnh truyền máu", Description = "Tạo mới, chỉnh sửa phiếu dự trù và yêu cầu máu bệnh nhân" },
            new Permission { PermissionCode = "W4_APPROVE", PermissionName = "Phê duyệt y lệnh truyền máu", Description = "Bác sĩ trưởng khoa duyệt y lệnh truyền máu khẩn cấp/thường quy" },
            new Permission { PermissionCode = "W4_CANCEL", PermissionName = "Hủy y lệnh truyền máu", Description = "Hủy bỏ y lệnh truyền máu khi bệnh nhân đổi chỉ định" },

            new Permission { PermissionCode = "W5_VIEW", PermissionName = "Xem cấp phát & truyền máu", Description = "Tra cứu lịch sử cấp phát túi máu và giám sát truyền máu" },
            new Permission { PermissionCode = "W5_EDIT", PermissionName = "Cập nhật cấp phát & truyền máu", Description = "Ghi nhận phản ứng chéo, bàn giao túi máu và diễn tiến truyền máu" },
            new Permission { PermissionCode = "W5_APPROVE", PermissionName = "Phê duyệt cấp phát túi máu", Description = "Duyệt xuất kho cấp phát túi máu cho khoa lâm sàng" },
            new Permission { PermissionCode = "W5_CANCEL", PermissionName = "Thu hồi / Hoàn trả túi máu", Description = "Xử lý trả máu chưa sử dụng về ngân hàng máu" },

            new Permission { PermissionCode = "W6_VIEW", PermissionName = "Xem cấu hình hệ thống", Description = "Xem danh sách người dùng, vai trò, nhật ký audit log" },
            new Permission { PermissionCode = "W6_EDIT", PermissionName = "Quản trị người dùng & vai trò", Description = "Thêm/sửa người dùng, phân quyền vai trò, đổi tham số hệ thống" },
            new Permission { PermissionCode = "W6_APPROVE", PermissionName = "Phê duyệt cấu hình hệ thống", Description = "Duyệt thay đổi cấu hình bảo mật và kích hoạt tính năng" },
            new Permission { PermissionCode = "W6_CANCEL", PermissionName = "Khóa / Thu hồi truy cập", Description = "Khóa tài khoản, vô hiệu hóa vai trò hoặc cấu hình hệ thống" },
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
