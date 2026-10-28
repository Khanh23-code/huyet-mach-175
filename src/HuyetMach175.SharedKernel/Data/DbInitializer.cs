using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Services;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        await EnsureSchemaUpdatedAsync(context);

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

            new Permission { PermissionCode = "USERS_VIEW", PermissionName = "Xem danh sách Người dùng", ModuleCode = "USERS", ModuleName = "Người dùng", ActionType = PermissionAction.VIEW, Description = "Quản lý tài khoản nhân viên" },
            new Permission { PermissionCode = "USERS_EDIT", PermissionName = "Tạo mới / Sửa / Khóa Người dùng", ModuleCode = "USERS", ModuleName = "Người dùng", ActionType = PermissionAction.EDIT, Description = "Quản lý tài khoản nhân viên" },

            new Permission { PermissionCode = "ROLES_VIEW", PermissionName = "Xem Vai trò và Ma trận phân quyền", ModuleCode = "ROLES", ModuleName = "Vai trò & Phân quyền", ActionType = PermissionAction.VIEW, Description = "Quản lý vai trò và phân quyền" },
            new Permission { PermissionCode = "ROLES_EDIT", PermissionName = "Tạo / Sửa Vai trò và Phân quyền", ModuleCode = "ROLES", ModuleName = "Vai trò & Phân quyền", ActionType = PermissionAction.EDIT, Description = "Quản lý vai trò và phân quyền" },
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
                "DONORS_VIEW", "APPOINTMENTS_VIEW", "SURVEY_VIEW", "SCREENING_VIEW", "SESSION_VIEW"
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
                "CAMPAIGN_VIEW", "CAMPAIGN_EDIT", "DONORS_VIEW", "SCREENING_VIEW", "SESSION_VIEW"
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

        await SeedAppointmentsAndSurveysAsync(context);
    }

    private static async Task EnsureSchemaUpdatedAsync(ApplicationDbContext context)
    {
        const string sql = @"
            ALTER TABLE donation_appointments ADD COLUMN IF NOT EXISTS slot_id integer NULL;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_donation_appointments_donation_campaign_slots_slot_id'
                ) THEN
                    ALTER TABLE donation_appointments
                    ADD CONSTRAINT ""FK_donation_appointments_donation_campaign_slots_slot_id""
                    FOREIGN KEY (slot_id) REFERENCES donation_campaign_slots(slot_id) ON DELETE SET NULL;
                END IF;
            END $$;

            CREATE INDEX IF NOT EXISTS ""IX_donation_appointments_slot_id"" ON donation_appointments(slot_id);

            DELETE FROM pre_screening_surveys
            WHERE appointment_id IN (SELECT appointment_id FROM donation_appointments WHERE slot_id IS NULL);

            DELETE FROM donation_sessions
            WHERE appointment_id IN (SELECT appointment_id FROM donation_appointments WHERE slot_id IS NULL)
              AND session_id NOT IN (SELECT session_id FROM blood_bags WHERE session_id IS NOT NULL);

            DELETE FROM donation_appointments
            WHERE slot_id IS NULL
              AND appointment_id NOT IN (SELECT appointment_id FROM donation_sessions);

            ALTER TABLE donation_appointments DROP CONSTRAINT IF EXISTS ""FK_donation_appointments_donation_campaigns_campaign_id"";
            DROP INDEX IF EXISTS ""IX_donation_appointments_campaign_id"";
            ALTER TABLE donation_appointments DROP COLUMN IF EXISTS campaign_id;
            ALTER TABLE donation_appointments DROP COLUMN IF EXISTS time_slot;

            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS survey_submitted_at timestamp with time zone NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS doctor_id integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS weight_kg numeric(5,2) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS systolic_blood_pressure integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS diastolic_blood_pressure integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS pulse integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS hemoglobin_level numeric(5,2) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS body_temp numeric(4,1) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS rapid_hbsag_negative boolean NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS clinical_notes character varying(500) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS screening_status text NOT NULL DEFAULT 'PENDING';
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS deferral_reason character varying(500) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS target_volume_ml text NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS screened_at timestamp with time zone NULL;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_pre_screening_surveys_users_doctor_id'
                ) THEN
                    ALTER TABLE pre_screening_surveys
                    ADD CONSTRAINT ""FK_pre_screening_surveys_users_doctor_id""
                    FOREIGN KEY (doctor_id) REFERENCES users(user_id) ON DELETE RESTRICT;
                END IF;
            END $$;

            ALTER TABLE donation_sessions ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE donation_sessions ALTER COLUMN collection_incident TYPE character varying(500);
            ALTER TABLE donation_sessions DROP CONSTRAINT IF EXISTS ""FK_donation_sessions_users_doctor_id"";
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS doctor_id;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS weight_kg;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS blood_pressure;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS hemoglobin_level;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS screening_status;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS deferral_reason;
        ";

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedAppointmentsAndSurveysAsync(ApplicationDbContext context)
    {
        if (await context.DonationAppointments.AnyAsync(da => da.QrCodeToken.StartsWith("HM175-SEED-")))
        {
            return;
        }

        var donorIds = await context.Donors
            .Where(d => d.DonorId >= 1 && d.DonorId <= 13)
            .OrderBy(d => d.DonorId)
            .Select(d => d.DonorId)
            .ToListAsync();

        var slotIds = await context.DonationCampaignSlots
            .Where(s => s.SlotId >= 3 && s.SlotId <= 8)
            .OrderBy(s => s.SlotId)
            .Select(s => s.SlotId)
            .ToListAsync();

        if (donorIds.Count == 0 || slotIds.Count == 0)
        {
            return;
        }

        var doctorId = await context.Users
            .Where(u => u.Username == "doctor")
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var nowUtc = DateTime.UtcNow;

        const string safeSurveyJson = @"{""q1_previous_donation"":true,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":false,""q5_last_14_days"":false,""q6_last_7_days"":false,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";
        const string riskSurveyJson1 = @"{""q1_previous_donation"":true,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":false,""q5_last_14_days"":true,""q6_last_7_days"":false,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";
        const string riskSurveyJson2 = @"{""q1_previous_donation"":false,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":true,""q5_last_14_days"":false,""q6_last_7_days"":true,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";

        for (int i = 0; i < donorIds.Count; i++)
        {
            var donorId = donorIds[i];
            var slotId = slotIds[i % slotIds.Count];
            var index1Based = i + 1;

            var status = AppointmentStatus.CHECKED_IN;
            var isWalkIn = false;
            DateTime? checkedInAt = nowUtc.AddMinutes(-90 + i * 5);

            if (index1Based is 7 or 8)
            {
                isWalkIn = true;
            }
            else if (index1Based is 11 or 12)
            {
                status = AppointmentStatus.BOOKED;
                checkedInAt = null;
            }
            else if (index1Based == 13)
            {
                status = AppointmentStatus.CANCELLED;
                checkedInAt = null;
            }

            var appointment = new DonationAppointment
            {
                DonorId = donorId,
                SlotId = slotId,
                AppointmentDate = today,
                QrCodeToken = $"HM175-SEED-{donorId:D4}-{slotId:D2}",
                Status = status,
                IsWalkIn = isWalkIn,
                CheckedInAt = checkedInAt,
                CreatedAt = nowUtc.AddHours(-3)
            };

            context.DonationAppointments.Add(appointment);
            await context.SaveChangesAsync();

            if (index1Based is >= 1 and <= 4 or 11 or 12)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = safeSurveyJson,
                    RiskScore = 0,
                    HasRisk = false,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    ScreeningStatus = ScreeningStatus.PENDING
                });
            }
            else if (index1Based == 5)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = riskSurveyJson1,
                    RiskScore = 1,
                    HasRisk = true,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    ScreeningStatus = ScreeningStatus.PENDING
                });
            }
            else if (index1Based == 6)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = riskSurveyJson2,
                    RiskScore = 2,
                    HasRisk = true,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    ScreeningStatus = ScreeningStatus.PENDING
                });
            }
            else if (index1Based == 9)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = safeSurveyJson,
                    RiskScore = 0,
                    HasRisk = false,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    DoctorId = doctorId,
                    WeightKg = 68.5m,
                    SystolicBloodPressure = 120,
                    DiastolicBloodPressure = 78,
                    Pulse = 74,
                    HemoglobinLevel = 142.0m,
                    BodyTemp = 36.6m,
                    RapidHbsAgNegative = true,
                    ClinicalNotes = "Tỉnh táo, huyết động ổn định, đủ điều kiện hiến máu 350ml.",
                    ScreeningStatus = ScreeningStatus.QUALIFIED,
                    TargetVolumeMl = TargetVolume.VOL_350,
                    ScreenedAt = nowUtc.AddMinutes(-20)
                });

                context.DonationSessions.Add(new DonationSession
                {
                    AppointmentId = appointment.AppointmentId,
                    TargetVolumeMl = TargetVolume.VOL_350,
                    Status = SessionStatus.WAITING_COLLECTION,
                    CreatedAt = nowUtc.AddMinutes(-20)
                });
            }
            else if (index1Based == 10)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = riskSurveyJson1,
                    RiskScore = 1,
                    HasRisk = true,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    DoctorId = doctorId,
                    WeightKg = 58.0m,
                    SystolicBloodPressure = 152,
                    DiastolicBloodPressure = 96,
                    Pulse = 92,
                    HemoglobinLevel = 130.0m,
                    BodyTemp = 36.9m,
                    RapidHbsAgNegative = true,
                    ClinicalNotes = "Huyết áp cao tại thời điểm khám, mới khỏi cảm cúm.",
                    ScreeningStatus = ScreeningStatus.DEFERRED,
                    DeferralReason = "Huyết áp tâm thu cao (152/96 mmHg) và mới khỏi cảm cúm trong 14 ngày.",
                    ScreenedAt = nowUtc.AddMinutes(-15)
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
