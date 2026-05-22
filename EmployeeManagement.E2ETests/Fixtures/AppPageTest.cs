using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace EmployeeManagement.E2ETests.Fixtures;

[Parallelizable(ParallelScope.None)]
public abstract class AppPageTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = GlobalAppHost.BaseUrl,
        Locale = "ja-JP",
        ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
        IgnoreHTTPSErrors = true,
    };

    [SetUp]
    public async Task RestoreDbAndStartTracingAsync()
    {
        // 各テストの独立性を保つため、変更系・読み取り系を問わず DB をシード状態へ戻す。
        await DatabaseReset.RestoreSeedAsync();
        // SQL Server の DELETE/INSERT が ASP.NET 側のコネクションプールに伝播するまでの軽い待ち。
        await Task.Delay(200);

        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task StopTracingAsync()
    {
        var failed = TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed;
        if (failed)
        {
            Directory.CreateDirectory(TraceDir);
            var safeName = SanitizeFileName(TestContext.CurrentContext.Test.FullName);
            await Context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(TraceDir, $"{safeName}.zip"),
            });
        }
        else
        {
            await Context.Tracing.StopAsync();
        }
    }

    protected string ScreenshotDir
    {
        get
        {
            var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    protected string TraceDir => Path.Combine(TestContext.CurrentContext.WorkDirectory, "traces");

    protected Task SnapAsync(string caseId, int step, string label)
    {
        var safeLabel = SanitizeFileName(label);
        var path = Path.Combine(ScreenshotDir, $"{caseId}_step{step}_{safeLabel}.png");
        return Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true,
        });
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Length > 120 ? sanitized.Substring(0, 120) : sanitized;
    }
}
