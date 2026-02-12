using Microsoft.EntityFrameworkCore;
<<<<<<< HEAD
using School.API.Data.DBModels;
using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
=======
using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.Data.DBModels.HR;
using School.API.DTOs.Academic;
using School.API.DTOs.Accounts;
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68
using School.API.DTOs.Common;
using School.API.Models;

namespace School.API.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
        }
<<<<<<< HEAD
        
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<SMSClass> SMSClasses { get; set; }
        public DbSet<SMSSection> SMSSections { get; set; }
        public DbSet<SMSSubject> SMSSubjects { get; set; }
        public DbSet<ResponseDto> AcademicSessionYearResponses { get; set; }

        public DbSet<AcademicSessionYear> AcademicSessionYear { get; set; }
=======

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
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<TimeTable> TimeTables { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }

        // HR entities
        public DbSet<Designation> Designations { get; set; }
        public DbSet<HRGrade> HRGrades { get; set; }
        //public DbSet<AcademicGrade> AcademicGrades { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Salary> Salaries { get; set; }

        // Accounts entities
        public DbSet<FeeType> FeeTypes { get; set; }
        public DbSet<FeeStructure> FeeStructures { get; set; }
        //public DbSet<PaymentMethod> PaymentMethods { get; set; }
        //public DbSet<ChallanVoucher> ChallanVouchers { get; set; }
        //public DbSet<FeeVoucher> FeeVouchers { get; set; }

        // Keyless entities
        public DbSet<ResponseDto> AcademicSessionYearResponses { get; set; }
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keyless entity
            modelBuilder.Entity<ResponseDto>().HasNoKey();
            modelBuilder.Entity<AcademicSessionYearSaveDto>().HasNoKey();
<<<<<<< HEAD
=======
            modelBuilder.Entity<SMSClassSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSSectionSaveDto>().HasNoKey();
            modelBuilder.Entity<FeeTypeSaveDto>().HasNoKey();
            modelBuilder.Entity<FeeStructureSaveDto>().HasNoKey();
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68

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
<<<<<<< HEAD
=======

            // Student configuration
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasOne(s => s.Admission)
                    .WithMany()
                    .HasForeignKey(s => s.AdmissionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68
        }
    }
}
