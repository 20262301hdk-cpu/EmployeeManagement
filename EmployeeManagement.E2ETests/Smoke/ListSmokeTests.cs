using System.Text.RegularExpressions;
using EmployeeManagement.E2ETests.Fixtures;
using EmployeeManagement.E2ETests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace EmployeeManagement.E2ETests.Smoke;

[TestFixture]
[Category("Full")]
public sealed class ListSmokeTests : AppPageTest
{
    [SetUp]
    public Task LoginAsUserAsync() =>
        LoginHelper.LoginAsync(Page, SeedConstants.Suzuki.Email, SeedConstants.Suzuki.Password);

    // T10 はテスト間 DB 状態同期問題で Smoke では不安定なため Full のみに残す。
    [Test]
    public async Task T10_ListShowsAllSeedEmployees()
    {
        await Page.GotoAsync("/Employees");

        foreach (var seed in SeedConstants.All)
        {
            await Expect(Page.GetByText(seed.EmpName).First).ToBeVisibleAsync();
        }

        await SnapAsync("T-10", 1, "list_all");
    }

    // T-12 (Smoke): 部署検索 UI 自体はチャレンジ課題 (F-05) のため、
    // 課題範囲のみ実装した状態でも一覧に経理部の社員が含まれていることを確認する緩い検証。
    [Test]
    [Category("Smoke")]
    public async Task T12_ListIncludesAccountingMember()
    {
        await Page.GotoAsync("/Employees");

        await Expect(Page.GetByText(SeedConstants.Tanaka.EmpName).First).ToBeVisibleAsync();

        await SnapAsync("T-12", 1, "list_includes_accounting");
    }

    // T-12C (Challenge): 部署検索 UI を実装した後の厳密検証。チャレンジ未実装時は実行されない。
    [Test]
    [Category("Challenge")]
    public async Task T12C_FilterByDepartment_AccountingShowsOnlyTanaka()
    {
        await Page.GotoAsync("/Employees");
        await Page.GetByLabel("部署").SelectOptionAsync(new SelectOptionValue { Label = SeedConstants.DeptAccounting });
        await Page.GetByRole(AriaRole.Button, new() { Name = "検索" }).ClickAsync();

        await Expect(Page.GetByText(SeedConstants.Tanaka.EmpName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(SeedConstants.Suzuki.EmpName)).Not.ToBeVisibleAsync();

        await SnapAsync("T-12C", 1, "search_by_dept");
    }

    [Test]
    [Category("Smoke")]
    public async Task T14_DetailsLinkOpensDetailPage()
    {
        await Page.GotoAsync("/Employees");
        var row = Page.GetByRole(AriaRole.Row, new() { Name = SeedConstants.Suzuki.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "詳細" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/Employees/Details/\d+"));
        await Expect(Page.GetByText(SeedConstants.Suzuki.EmpName).First).ToBeVisibleAsync();

        await SnapAsync("T-14", 1, "details");
    }
}
