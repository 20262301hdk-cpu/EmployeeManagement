using EmployeeManagement.Data;
using EmployeeManagement.E2ETests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.E2ETests.Fixtures;

internal static class DatabaseReset
{
    public static async Task RestoreSeedAsync(CancellationToken ct = default)
    {
        var connectionString = LoadTestConnectionString();
        var services = BuildServices(connectionString);
        await using var provider = services.BuildServiceProvider();

        await ApplyMigrationsAsync(provider, ct);
        await DeleteNonSeedRowsAsync(provider, ct);
        await Program.SeedAsync(provider);
    }

    public static async Task FullResetAsync(CancellationToken ct = default)
    {
        // EmployeeManagementDb_Test を丸ごと削除 → MigrateAsync で再作成 → SeedAsync。
        // RestoreSeedAsync は外部キー制約や Identity キャッシュで Suzuki が再作成
        // されないことが稀にあるため、明示的にクリーンスタートしたいテストで利用する。
        // ASP アプリが起動中だと EnsureDeletedAsync が失敗するため、呼び出す側で
        // GlobalAppHost が動いていないタイミングを保証すること。
        var connectionString = LoadTestConnectionString();
        var services = BuildServices(connectionString);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.MigrateAsync(ct);
        await Program.SeedAsync(provider);
    }

    private static string LoadTestConnectionString()
    {
        var basePath = AppContext.BaseDirectory;
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
            .Build();

        return config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("appsettings.Test.json に DefaultConnection が見つかりません。");
    }

    private static IServiceCollection BuildServices(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddSimpleConsole());

        // UserManager は内部で IDataProtectionProvider を要求するため明示登録が必要。
        services.AddDataProtection();
        services.AddHttpContextAccessor();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentityCore<IdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddRoleManager<RoleManager<IdentityRole>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider provider, CancellationToken ct)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    private static async Task DeleteNonSeedRowsAsync(IServiceProvider provider, CancellationToken ct)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 完全リセット方針: Employees / AspNetUsers / Departments を毎回全削除し、
        // SeedAsync が 3 ロール / 3 部署 / 3 ユーザー / 3 社員を再作成する。
        // ロール (AspNetRoles) は残す (SeedAsync の RoleExistsAsync 守りで冪等)。
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Employees", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUserRoles", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Departments", ct);
    }
}
