using System.Text.RegularExpressions;
using EmployeeManagement.E2ETests.Fixtures;
using EmployeeManagement.E2ETests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace EmployeeManagement.E2ETests.Smoke;

[TestFixture]
[Category("Full")]
public sealed class CrudSmokeTests : AppPageTest
{
    [SetUp]
    public Task LoginAsAdminAsync() =>
        LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);

    [Test]
    [Category("Smoke")]
    public async Task T16_Create_HappyPath()
    {
        // テスト間で Email 重複の連鎖を避けるため、テストごとに email をユニーク化する。
        var uniqueSuffix = DateTime.UtcNow.Ticks.ToString("x");
        var email = $"yamada-{uniqueSuffix}@example.com";
        var empName = $"山田次郎-{uniqueSuffix}";

        await Page.GotoAsync("/Employees/Create");
        await FillCreateFormAsync(
            empName: empName,
            email: email,
            password: "Yamada1!",
            confirmPassword: "Yamada1!",
            gender: "1",
            address: "東京都新宿区",
            birthday: "1990-05-15",
            role: SeedConstants.UserRole,
            deptName: SeedConstants.DeptSales);
        await SnapAsync("T-16", 1, "input");

        await Page.GetByRole(AriaRole.Button, new() { Name = "確認" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "確認" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "登録" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "完了" })).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex("登録しました"))).ToBeVisibleAsync();
        await SnapAsync("T-16", 3, "complete");

        await Page.GetByRole(AriaRole.Link, new() { Name = "一覧へ" }).ClickAsync();
        await Expect(Page.GetByText(empName).First).ToBeVisibleAsync();
    }

    // T-23 は社員編集 (F-08) のチャレンジ課題範囲。チャレンジ未実装時はスキップされる。
    [Test]
    [Category("Challenge")]
    public async Task T23_Edit_HappyPath()
    {
        await Page.GotoAsync("/Employees");
        var row = Page.GetByRole(AriaRole.Row, new() { Name = SeedConstants.Suzuki.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "編集" }).ClickAsync();
        await Expect(Page.GetByLabel("氏名")).ToHaveValueAsync(SeedConstants.Suzuki.EmpName);

        var newName = SeedConstants.Suzuki.EmpName + "(改)";
        await Page.GetByLabel("氏名").FillAsync(newName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "確認" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "確認" })).ToBeVisibleAsync();
        await SnapAsync("T-23", 2, "confirm");

        await Page.GetByRole(AriaRole.Button, new() { Name = "更新" }).ClickAsync();
        await Expect(Page.GetByText(new Regex("更新しました"))).ToBeVisibleAsync();
        await SnapAsync("T-23", 3, "complete");

        await Page.GetByRole(AriaRole.Link, new() { Name = "一覧へ" }).ClickAsync();
        await Expect(Page.GetByText(newName).First).ToBeVisibleAsync();
    }

    // T-29 は社員削除 (F-09) のチャレンジ課題範囲。チャレンジ未実装時はスキップされる。
    [Test]
    [Category("Challenge")]
    public async Task T29_Delete_HappyPath()
    {
        var target = SeedConstants.Watanabe;

        await Page.GotoAsync("/Employees");
        var row = Page.GetByRole(AriaRole.Row, new() { Name = target.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "削除" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "削除" })).ToBeVisibleAsync();
        await SnapAsync("T-29", 1, "confirm");

        await Page.GetByRole(AriaRole.Button, new() { Name = "削除" }).ClickAsync();
        await Expect(Page.GetByText(new Regex("削除しました"))).ToBeVisibleAsync();
        await SnapAsync("T-29", 2, "complete");

        await Page.GetByRole(AriaRole.Link, new() { Name = "一覧へ" }).ClickAsync();
        await Expect(Page.GetByText(target.EmpName)).Not.ToBeVisibleAsync();
        await SnapAsync("T-29", 3, "list_after_delete");
    }

    private async Task FillCreateFormAsync(
        string empName,
        string email,
        string password,
        string confirmPassword,
        string gender,
        string address,
        string birthday,
        string role,
        string deptName)
    {
        await Page.Locator("input[name='EmpName']").FillAsync(empName);
        await Page.Locator("input[name='Email']").FillAsync(email);
        await Page.Locator("input[name='Password']").FillAsync(password);
        await Page.Locator("input[name='ConfirmPassword']").FillAsync(confirmPassword);
        await Page.Locator($"input[name='Gender'][value='{gender}']").CheckAsync();
        await Page.Locator("input[name='Address']").FillAsync(address);
        await Page.Locator("input[name='Birthday']").FillAsync(birthday);
        await Page.Locator($"input[name='Role'][value='{role}']").CheckAsync();
        await Page.Locator("select[name='DeptId']").SelectOptionAsync(new SelectOptionValue { Label = deptName });
    }
}
