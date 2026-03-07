using Microsoft.EntityFrameworkCore;
using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.Data.DBModels.HR;
using School.API.Data.DBModels.SMS;
using School.API.Data.DBModels.Student;
using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.DTOs.SMS;
using School.API.Models;

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
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }

        // HR entities
        public DbSet<Designation> Designations { get; set; }
        public DbSet<HRGrade> HRGrades { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Salary> Salaries { get; set; }

        public DbSet<SMSCampus> Campuses { get; set; }
        public DbSet<StudentStatus> StudentStatuses { get; set; }

        // Accounts entities
        public DbSet<SMSFeeType> FeeTypes { get; set; }
        public DbSet<FeeStructure> FeeStructures { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }

        // SMS entities
        public DbSet<SmsMessage> SmsMessages { get; set; }

        // Keyless entities ï¿½ for SP results
        public DbSet<ResponseDto> AcademicSessionYearResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<StudentFeePreviewResultDto>().HasNoKey();
            modelBuilder.Entity<ResponseDto>().HasNoKey();

            // SchoolDbContext.cs — OnModelCreating
            modelBuilder.Entity<StudentComboDto>().HasNoKey();

            // SchoolDbContext.cs — OnModelCreating
            modelBuilder.Entity<StudentFeeListDto>().HasNoKey();

            //  Keyless entities (SP result DTOs) 
            modelBuilder.Entity<ResponseDto>().HasNoKey();
            modelBuilder.Entity<AcademicSessionYearSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSClassSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSSectionSaveDto>().HasNoKey();
            modelBuilder.Entity<AcademicGradeSaveDto>().HasNoKey();
            modelBuilder.Entity<FeeTypeSaveDto>().HasNoKey();
            modelBuilder.Entity<StudentSaveDto>().HasNoKey();
            modelBuilder.Entity<FeeStructureSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSCampusSaveDto>().HasNoKey();
            modelBuilder.Entity<StudentStatusSaveDto>().HasNoKey();

            //  New keyless DTOs for SMS 
            modelBuilder.Entity<SmsCountDto>().HasNoKey();

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
        }
    }
}