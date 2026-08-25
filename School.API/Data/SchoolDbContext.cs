using Microsoft.EntityFrameworkCore;
using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.Data.DBModels.FeeManagement;
using School.API.Data.DBModels.HR;
using School.API.Data.DBModels.Settings;
using School.API.Data.DBModels.SMS;
using School.API.Data.DBModels.Student;
using School.API.Data.DBModels.UserManagement;
using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.DTOs.SMS;
using School.API.DTOs.UserManagement;
using School.API.Models;
using FeeVoucherListDto = School.API.DTOs.FeeManagement.FeeVoucherListDto;

namespace School.API.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
        }

        // Core entities
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // Academic entities
        public DbSet<SMSClass> SMSClasses { get; set; }
        public DbSet<SMSSection> SMSSections { get; set; }
        public DbSet<SMSSubject> SMSSubjects { get; set; }
        public DbSet<AcademicSessionYear> AcademicSessionYears { get; set; }
        public DbSet<AcademicGrades> AcademicGrades { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<StdAttendence> StdAttendences { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<TimeTable> TimeTables { get; set; }

        // HR entities
        public DbSet<Designation> Designations { get; set; }
        public DbSet<HRGrade> HRGrades { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Teacher> Teachers { get; set; }

        // HR Payroll (HR-Payroll-Final-Plan.md section 3)
        public DbSet<HrShift> HrShifts { get; set; }
        public DbSet<HrHoliday> HrHolidays { get; set; }
        public DbSet<HrLeaveType> HrLeaveTypes { get; set; }
        public DbSet<HrAttendance> HrAttendances { get; set; }
        public DbSet<HrLeave> HrLeaves { get; set; }
        public DbSet<HrPayComponent> HrPayComponents { get; set; }
        public DbSet<HrEmployeePay> HrEmployeePays { get; set; }
        public DbSet<HrPayrollRun> HrPayrollRuns { get; set; }
        public DbSet<HrPayslip> HrPayslips { get; set; }

        public DbSet<SMSCampus> Campuses { get; set; }
        public DbSet<StudentStatus> StudentStatuses { get; set; }

        // Settings
        public DbSet<BackupJobSetting> BackupJobSettings { get; set; }

        // Accounts entities
        public DbSet<SMSFeeType> FeeTypes { get; set; }
        public DbSet<FeeStructure> FeeStructures { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }

        // SMS entities
        public DbSet<SmsMessage> SmsMessages { get; set; }

        // WhatsApp two-way chat (built from Meta webhook events + our sends)
        public DbSet<WaConversation> WaConversations { get; set; }
        public DbSet<WaMessage> WaMessages { get; set; }

        // Keyless entities � for SP results
        public DbSet<ResponseDto> AcademicSessionYearResponses { get; set; }

        // 1. Add to DbSets (with the rest of your DbSet properties)
        public DbSet<SecUser> SecUsers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


        
            // 2. Add inside OnModelCreating method (with your other modelBuilder configs)
            modelBuilder.Entity<SecUser>().HasNoKey().ToView(null);
            modelBuilder.Entity<UserLoginResponseDto>().HasNoKey().ToView(null);

            modelBuilder.Entity<StudentFeePreviewResultDto>().HasNoKey();
            modelBuilder.Entity<ResponseDto>().HasNoKey();

            // SchoolDbContext.cs � OnModelCreating
            modelBuilder.Entity<StudentComboDto>().HasNoKey();

            // SchoolDbContext.cs � OnModelCreating
            modelBuilder.Entity<StudentFeeListDto>().HasNoKey();
            modelBuilder.Entity<StudentFeeReportDto>().HasNoKey();
            modelBuilder.Entity<VNoDto>().HasNoKey().ToView(null);
            // OnModelCreating mein
            modelBuilder.Entity<FeeVoucherRptRow>().HasNoKey().ToView(null);
            modelBuilder.Entity<FeeVoucherMainRow>().HasNoKey().ToView(null);

            // In SchoolDbContext.cs OnModelCreating()
            modelBuilder.Entity<FeeVoucherListDto>().HasNoKey().ToView(null);
            modelBuilder.Entity<FeeVoucherMain>().HasNoKey().ToView(null);
            modelBuilder.Entity<FeeVoucherDetail>().HasNoKey().ToView(null);

            //  Keyless entities (SP result DTOs) 
            modelBuilder.Entity<ResponseDto>().HasNoKey();
            modelBuilder.Entity<AcademicSessionYearSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSClassSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSSectionSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSSubjectSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSExamSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSExamResultSaveDto>().HasNoKey();
            modelBuilder.Entity<AcademicGradeSaveDto>().HasNoKey();
            modelBuilder.Entity<FeeTypeSaveDto>().HasNoKey();
            modelBuilder.Entity<StudentSaveDto>().HasNoKey();
            modelBuilder.Entity<FeeStructureSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSCampusSaveDto>().HasNoKey();
            modelBuilder.Entity<StudentStatusSaveDto>().HasNoKey();

            //  New keyless DTOs for SMS
            modelBuilder.Entity<SmsCountDto>().HasNoKey();

            //  Staff Subjects (joined SP result row)
            modelBuilder.Entity<StaffSubjectRowDto>().HasNoKey();

            //  Table name mappings 
            modelBuilder.Entity<StdAttendence>().ToTable("StdAttendence");

            // Menu configuration
            modelBuilder.Entity<Menu>(entity =>
            {
                entity.HasOne(m => m.Parent)
                    .WithMany(m => m.Children)
                    .HasForeignKey(m => m.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // RolePermission configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Menu)
                    .WithMany()
                    .HasForeignKey(rp => rp.MenuId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasOne(u => u.Employee)
                    .WithOne(e => e.User)
                    .HasForeignKey<User>(u => u.EmployeeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // UserRole configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // HR Payroll — owned JSON columns (D3: JSON instead of child tables).
            modelBuilder.Entity<HrPayComponent>(entity =>
            {
                entity.OwnsMany(c => c.Slabs, slab => slab.ToJson("SlabsJson"));
            });
            modelBuilder.Entity<HrEmployeePay>(entity =>
            {
                entity.OwnsMany(p => p.Components, comp => comp.ToJson("ComponentsJson"));
            });
            modelBuilder.Entity<HrPayslip>(entity =>
            {
                entity.OwnsMany(p => p.Breakdown, line => line.ToJson("BreakdownJson"));
            });

            // No cross-context navigations were declared on the HR Payroll entities
            // (HrAttendance.EmployeeId, HrLeave.EmployeeId, etc. are plain FK ints) —
            // deliberate, keeps the payroll run's queries simple and predictable
            // (section 9) instead of triggering unexpected EF Include() fan-out.
        }
    }
}