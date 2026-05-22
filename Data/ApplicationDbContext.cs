using EmployeeManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 事前実装 (削減版): T-03 のリレーション設定は完成版から提供済み
        builder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.DeptId);
            entity.Property(d => d.DeptName).HasMaxLength(15).IsRequired();

            entity.HasData(
                new Department { DeptId = 1, DeptName = "営業部" },
                new Department { DeptId = 2, DeptName = "経理部" },
                new Department { DeptId = 3, DeptName = "総務部" });
        });

        builder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmpId);
            entity.Property(e => e.AspNetUserId).IsRequired();
            entity.Property(e => e.EmpName).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(60).IsRequired();
            entity.Property(e => e.Birthday).HasColumnType("date").IsRequired();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AspNetUser)
                .WithOne()
                .HasForeignKey<Employee>(e => e.AspNetUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.AspNetUserId).IsUnique();
        });
    }
}
