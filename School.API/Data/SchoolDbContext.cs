using Microsoft.EntityFrameworkCore;
using School.API.DTOs.Common;
using School.API.Models;

namespace School.API.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
        }

        public DbSet<Menu> Menus { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public object AcademicSessionYears { get; internal set; }
        public DbSet<ResponseDto> AcademicSessionYearResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
