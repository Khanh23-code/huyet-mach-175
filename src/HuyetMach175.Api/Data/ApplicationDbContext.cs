using Microsoft.EntityFrameworkCore;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // NHÓM A: AUTH & RBAC
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // NHÓM B: DONATION DOMAIN
    public DbSet<DonationCampaign> DonationCampaigns => Set<DonationCampaign>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<DonationAppointment> DonationAppointments => Set<DonationAppointment>();
    public DbSet<PreScreeningSurvey> PreScreeningSurveys => Set<PreScreeningSurvey>();
    public DbSet<DonationSession> DonationSessions => Set<DonationSession>();

    // NHÓM C: BLOOD BANK INVENTORY
    public DbSet<BloodComponentType> BloodComponentTypes => Set<BloodComponentType>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<BloodBag> BloodBags => Set<BloodBag>();
    public DbSet<BloodTestResult> BloodTestResults => Set<BloodTestResult>();

    // NHÓM D: CLINICAL ALLOCATION
    public DbSet<BloodRequest> BloodRequests => Set<BloodRequest>();
    public DbSet<BloodRequestItem> BloodRequestItems => Set<BloodRequestItem>();
    public DbSet<BloodAllocation> BloodAllocations => Set<BloodAllocation>();
    public DbSet<BloodReturn> BloodReturns => Set<BloodReturn>();

    // NHÓM E: AUDIT & COMPLIANCE
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động tìm và nạp tất cả các lớp IEntityTypeConfiguration trong Assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
