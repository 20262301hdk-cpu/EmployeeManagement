using EmployeeManagement.Data;
using EmployeeManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    internal static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 部署は ApplicationDbContext.OnModelCreating の HasData() でマイグレーション経由投入される。
        // ここでは Identity ユーザーと Employee のみ起動時に冪等補充する。
        await CreateUserAndEmployeeAsync(userManager, db, "suzuki@example.com",    "Suzuki1!",    "User",  "鈴木太郎", 1, "東京都", new DateTime(1986, 10, 12), deptId: 1);
        await CreateUserAndEmployeeAsync(userManager, db, "tanaka@example.com",    "Tanaka2!",    "Admin", "田中二郎", 1, "千葉県", new DateTime(1979,  7,  2), deptId: 2);
        await CreateUserAndEmployeeAsync(userManager, db, "watanabe@example.com",  "Watanabe3!",  "Admin", "渡辺花子", 2, "大阪府", new DateTime(1988,  4, 23), deptId: 3);
    }

    private static async Task CreateUserAndEmployeeAsync(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db,
        string email,
        string password,
        string role,
        string empName,
        int gender,
        string address,
        DateTime birthday,
        int deptId)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" / ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        if (!await db.Employees.AnyAsync(e => e.AspNetUserId == user.Id))
        {
            db.Employees.Add(new Employee
            {
                AspNetUserId = user.Id,
                EmpName = empName,
                Gender = gender,
                Address = address,
                Birthday = birthday,
                DeptId = deptId
            });
            await db.SaveChangesAsync();
        }
    }
}
