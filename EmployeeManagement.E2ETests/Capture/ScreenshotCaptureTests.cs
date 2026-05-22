using System.Text.RegularExpressions;
using EmployeeManagement.E2ETests.Fixtures;
using EmployeeManagement.E2ETests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace EmployeeManagement.E2ETests.Capture;

[TestFixture]
[Category("Capture")]
public sealed class ScreenshotCaptureTests : AppPageTest
{
    private static readonly string OutputDir = ResolveOutputDir();

    private static string ResolveOutputDir()
    {
        // bin/Debug/net8.0/ から 6階層遡って 001_設計・製造/images/screens に保存する。
        //   net8.0 → bin/Debug → bin → E2ETests → EmployeeManagement → 10_ソースコード_編集用_1.0.0 → 001_設計・製造
        var here = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(
            Path.Combine(here, "..", "..", "..", "..", "..", "..", "images", "screens"));
        Directory.CreateDirectory(candidate);
        return candidate;
    }

    private Task CaptureAsync(string name) =>
        Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(OutputDir, $"{name}.png"),
            FullPage = true,
        });

    [SetUp]
    public async Task EnsureCleanSeedAsync()
    {
        // AppPageTest.[SetUp] で DB を再シードした後、アプリの EF コネクションプールに
        // 変更が伝播するまで追加で待つ。基底クラスの 200ms ではキャプチャ系で
        // 編集・削除画面の行が古い状態に残ることがあるため余裕を持たせる。
        // また、過去のテスト実行で Captureテスト前に Smoke 系が作った社員行が残る
        // ことがあるため、明示的にもう一度シード復元を呼ぶ。
        await DatabaseReset.RestoreSeedAsync();
        await Task.Delay(1500);
    }

    [Test]
    public async Task S01_Login_Empty()
    {
        await Page.GotoAsync("/Account/Login");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "ログイン" })).ToBeVisibleAsync();
        await CaptureAsync("S-01_login");
    }

    [Test]
    public async Task S01_Login_Error()
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByLabel("メールアドレス").FillAsync("unknown@example.com");
        await Page.GetByLabel("パスワード", new() { Exact = true }).FillAsync("Wrong1!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "ログイン" }).ClickAsync();
        await Expect(Page.GetByText(new Regex("メールアドレスまたはパスワード"))).ToBeVisibleAsync();
        await CaptureAsync("S-01_login_error");
    }

    [Test]
    public async Task S02_AccessDenied()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Suzuki.Email, SeedConstants.Suzuki.Password);
        await Page.GotoAsync("/Employees/Create");
        await Expect(Page).ToHaveURLAsync(new Regex("/Account/AccessDenied"));
        await CaptureAsync("S-02_access_denied");
    }

    [Test]
    public async Task S04_EmployeeList()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "社員一覧" })).ToBeVisibleAsync();
        await CaptureAsync("S-04_list");
    }

    [Test]
    public async Task S05_EmployeeDetails()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees");
        await Page.ReloadAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = SeedConstants.Suzuki.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "詳細" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Employees/Details/\d+"));
        await CaptureAsync("S-05_details");
    }

    [Test]
    public async Task S06_CreateInputEmpty()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees/Create");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "社員登録" })).ToBeVisibleAsync();
        await CaptureAsync("S-06_create_input");
    }

    [Test]
    public async Task S07_S08_CreateConfirmAndComplete()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees/Create");

        var uniqueSuffix = DateTime.UtcNow.Ticks.ToString("x");
        await Page.Locator("input[name='EmpName']").FillAsync($"山田次郎");
        await Page.Locator("input[name='Email']").FillAsync($"yamada-{uniqueSuffix}@example.com");
        await Page.Locator("input[name='Password']").FillAsync("Yamada1!");
        await Page.Locator("input[name='ConfirmPassword']").FillAsync("Yamada1!");
        await Page.Locator("input[name='Gender'][value='1']").CheckAsync();
        await Page.Locator("input[name='Address']").FillAsync("東京都新宿区");
        await Page.Locator("input[name='Birthday']").FillAsync("1990-05-15");
        await Page.Locator("input[name='Role'][value='User']").CheckAsync();
        await Page.Locator("select[name='DeptId']").SelectOptionAsync(new SelectOptionValue { Label = SeedConstants.DeptSales });

        await Page.GetByRole(AriaRole.Button, new() { Name = "確認" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "確認" })).ToBeVisibleAsync();
        await CaptureAsync("S-07_create_confirm");

        await Page.GetByRole(AriaRole.Button, new() { Name = "登録" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "完了" })).ToBeVisibleAsync();
        await CaptureAsync("S-08_create_complete");
    }

    [Test]
    public async Task S09_EditInput()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees");
        await Page.ReloadAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = SeedConstants.Suzuki.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "編集" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "社員編集" })).ToBeVisibleAsync();
        await CaptureAsync("S-09_edit_input");
    }

    [Test]
    public async Task S10_S11_EditConfirmAndComplete()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees");
        await Page.ReloadAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = SeedConstants.Suzuki.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "編集" }).ClickAsync();
        await Page.WaitForURLAsync(new Regex(@"/Employees/Edit/\d+"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "確認" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "確認" })).ToBeVisibleAsync();
        await CaptureAsync("S-10_edit_confirm");

        await Page.GetByRole(AriaRole.Button, new() { Name = "更新" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "完了" })).ToBeVisibleAsync();
        await CaptureAsync("S-11_edit_complete");
    }

    [Test]
    public async Task S12_S13_DeleteConfirmAndComplete()
    {
        await LoginHelper.LoginAsync(Page, SeedConstants.Tanaka.Email, SeedConstants.Tanaka.Password);
        await Page.GotoAsync("/Employees");
        await Page.ReloadAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = SeedConstants.Suzuki.EmpName });
        await row.GetByRole(AriaRole.Link, new() { Name = "削除" }).ClickAsync();
        await Page.WaitForURLAsync(new Regex(@"/Employees/Delete/\d+"));
        await CaptureAsync("S-12_delete_confirm");

        await Page.GetByRole(AriaRole.Button, new() { Name = "削除" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "完了" })).ToBeVisibleAsync();
        await CaptureAsync("S-13_delete_complete");
    }

    [Test]
    public async Task S99_Error()
    {
        await Page.GotoAsync("/Home/Error");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "エラー" })).ToBeVisibleAsync();
        await CaptureAsync("S-99_error");
    }
}
