using EmployeeManagement.E2ETests.Fixtures;
using EmployeeManagement.E2ETests.Helpers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace EmployeeManagement.E2ETests.Smoke;

[TestFixture]
[Category("Smoke")]
[Category("Full")]
public sealed class LoginSmokeTests : AppPageTest
{
    [Test]
    public async Task T01_LoginAsUser_RedirectsToEmployees()
    {
        var user = SeedConstants.Suzuki;
        await LoginHelper.LoginAsync(Page, user.Email, user.Password);

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Employees"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "社員一覧" })).ToBeVisibleAsync();

        await SnapAsync("T-01", 1, "list_after_user_login");
    }

    [Test]
    public async Task T04_LoginWithUnknownEmail_ShowsError()
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByLabel("メールアドレス").FillAsync("unknown@example.com");
        await Page.GetByLabel("パスワード", new() { Exact = true }).FillAsync("Anything1!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "ログイン" }).ClickAsync();

        await Expect(Page.GetByText(new System.Text.RegularExpressions.Regex("メールアドレスまたはパスワード"))).ToBeVisibleAsync();
        await SnapAsync("T-04", 1, "login_error_unknown_email");
    }
}
