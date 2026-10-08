using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Services;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        var adminDepartment = await context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentCode == "DEP_ADMIN");

        if (adminDepartment == null)
        {
            adminDepartment = new Department
            {
                DepartmentCode = "DEP_ADMIN",
                DepartmentName = "Trung tâm Công nghệ Thông tin & Quản trị",
                CreatedAt = DateTime.UtcNow
            };
            context.Departments.Add(adminDepartment);
            await context.SaveChangesAsync();
        }

        var defaultRoles = new List<Role>
        {
            new Role
            {
                RoleCode = RoleCode.SYS,
                RoleName = "Quản trị hệ thống",
                Description = "Quyền cao nhất toàn bộ hệ thống"
            },
            new Role
            {
                RoleCode = RoleCode.MGT,
                RoleName = "Ban Giám đốc / Quản lý",
                Description = "Xem báo cáo và phê duyệt chiến dịch"
            },
            new Role
            {
                RoleCode = RoleCode.CLIN,
                RoleName = "Bác sĩ / Nhân viên Lâm sàng",
                Description = "Tạo y lệnh và yêu cầu truyền máu"
            },
            new Role
            {
                RoleCode = RoleCode.BBNK,
                RoleName = "Nhân viên Ngân hàng máu",
                Description = "Quản lý kho máu, xét nghiệm và cấp phát"
            },
            new Role
            {
                RoleCode = RoleCode.DON,
                RoleName = "Nhân viên Tiếp nhận & Lấy máu",
                Description = "Tiếp nhận tình nguyện viên, hướng dẫn và thực hiện lấy máu"
            }
        };

        foreach (var role in defaultRoles)
        {
            var exists = await context.Roles.AnyAsync(r => r.RoleCode == role.RoleCode);
            if (!exists)
            {
                context.Roles.Add(role);
            }
        }
        await context.SaveChangesAsync();

        var defaultPasswordHash = passwordHasher.HashPassword("password123");
        var adminPasswordHash = passwordHasher.HashPassword("admin123");

        var usersToSeed = new[]
        {
            new
            {
                Username = "admin",
                PasswordHash = adminPasswordHash,
                FullName = "Quản trị viên Hệ thống",
                Email = "admin@huyetmach175.vn",
                PhoneNumber = "0900000001",
                RoleCode = RoleCode.SYS
            },
            new
            {
                Username = "manager",
                PasswordHash = defaultPasswordHash,
                FullName = "Ban Giám Đốc",
                Email = "manager@huyetmach175.vn",
                PhoneNumber = "0900000002",
                RoleCode = RoleCode.MGT
            },
            new
            {
                Username = "doctor",
                PasswordHash = defaultPasswordHash,
                FullName = "Bác sĩ Lâm Sàng",
                Email = "doctor@huyetmach175.vn",
                PhoneNumber = "0900000003",
                RoleCode = RoleCode.CLIN
            },
            new
            {
                Username = "bloodbank",
                PasswordHash = defaultPasswordHash,
                FullName = "Kỹ Thuật Viên Ngân Hàng Máu",
                Email = "bloodbank@huyetmach175.vn",
                PhoneNumber = "0900000004",
                RoleCode = RoleCode.BBNK
            },
            new
            {
                Username = "donorstaff",
                PasswordHash = defaultPasswordHash,
                FullName = "Nhân Viên Tiếp Nhận & Lấy Máu",
                Email = "donorstaff@huyetmach175.vn",
                PhoneNumber = "0900000005",
                RoleCode = RoleCode.DON
            }
        };

        foreach (var u in usersToSeed)
        {
            var user = await context.Users.FirstOrDefaultAsync(item => item.Username == u.Username);
            if (user == null)
            {
                user = new User
                {
                    DepartmentId = adminDepartment.DepartmentId,
                    Username = u.Username,
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
