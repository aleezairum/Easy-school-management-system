using Microsoft.EntityFrameworkCore;
using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.HR;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
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
        public DbSet<AcademicSessionYear> AcademicSessionYear { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<Student> Students { get; set; }

        // HR entities
        public DbSet<Designation> Designations { get; set; }
        public DbSet<HRGrade> HRGrades { get; set; }
        public DbSet<AcademicGrade> AcademicGrades { get; set; }
        public DbSet<Employee> Employees { get; set; }

        // Keyless entities
        public DbSet<ResponseDto> AcademicSessionYearResponses { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keyless entity
            modelBuilder.Entity<ResponseDto>().HasNoKey();
            modelBuilder.Entity<AcademicSessionYearSaveDto>().HasNoKey();
            modelBuilder.Entity<SMSSectionSaveDto>().HasNoKey();

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

            // Student configuration
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasOne(s => s.Admission)
                    .WithMany()
                    .HasForeignKey(s => s.AdmissionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
