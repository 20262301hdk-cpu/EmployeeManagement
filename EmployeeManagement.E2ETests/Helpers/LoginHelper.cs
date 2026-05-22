using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EmployeeManagement.E2ETests.Helpers;

internal static class LoginHelper
{
    public static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/Account/Login");
        await page.GetByLabel("メールアドレス").FillAsync(email);
        await page.GetByLabel("パスワード", new PageGetByLabelOptions { Exact = true }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログイン" }).ClickAsync();
        await page.WaitForURLAsync(new Regex("/Employees"));
    }

    public static async Task LogoutAsync(IPage page)
    {
        var logout = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログアウト" });
        if (await logout.CountAsync() == 0)
        {
            // フォーム submit が button ではなく link 表現の場合のフォールバック
            logout = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "ログアウト" });
        }

        await logout.First.ClickAsync();
        await page.WaitForURLAsync(new Regex("/Account/Login"));
    }
}
