namespace EmployeeManagement.E2ETests.Helpers;

internal static class SeedConstants
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public const string DeptSales = "営業部";
    public const string DeptAccounting = "経理部";
    public const string DeptGeneral = "総務部";

    public static readonly IReadOnlyList<string> DepartmentNames = new[]
    {
        DeptSales,
        DeptAccounting,
        DeptGeneral,
    };

    public sealed record SeedUser(
        string Email,
        string Password,
        string Role,
        string EmpName,
        int Gender,
        string Address,
        DateTime Birthday,
        string DeptName);

    public static readonly SeedUser Suzuki = new(
        Email: "suzuki@example.com",
        Password: "Suzuki1!",
        Role: UserRole,
        EmpName: "鈴木太郎",
        Gender: 1,
        Address: "東京都",
        Birthday: new DateTime(1986, 10, 12),
        DeptName: DeptSales);

    public static readonly SeedUser Tanaka = new(
        Email: "tanaka@example.com",
        Password: "Tanaka2!",
        Role: AdminRole,
        EmpName: "田中二郎",
        Gender: 1,
        Address: "千葉県",
        Birthday: new DateTime(1979, 7, 2),
        DeptName: DeptAccounting);

    public static readonly SeedUser Watanabe = new(
        Email: "watanabe@example.com",
        Password: "Watanabe3!",
        Role: AdminRole,
        EmpName: "渡辺花子",
        Gender: 2,
        Address: "大阪府",
        Birthday: new DateTime(1988, 4, 23),
        DeptName: DeptGeneral);

    public static readonly IReadOnlyList<SeedUser> All = new[] { Suzuki, Tanaka, Watanabe };

    public static readonly IReadOnlyList<string> SeedEmails = All.Select(u => u.Email).ToArray();
    public static readonly IReadOnlyList<string> SeedEmpNames = All.Select(u => u.EmpName).ToArray();
}
