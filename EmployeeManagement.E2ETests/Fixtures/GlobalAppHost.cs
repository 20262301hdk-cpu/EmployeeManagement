using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using EmployeeManagement.E2ETests.Fixtures;
using NUnit.Framework;

namespace EmployeeManagement.E2ETests;

// SetUpFixture は最寄りの祖先 namespace に置くと配下すべてに適用されるため、ここでは root に近い
// EmployeeManagement.E2ETests namespace に配置し、Fixtures / Helpers / Smoke / Full すべてに反映する。
[SetUpFixture]
public sealed class GlobalAppHost
{
    public const string BaseUrl = "http://localhost:5050";
    private const int Port = 5050;
    private const string LocalDbInstance = "MSSQLLocalDB";
    private const int ReadyTimeoutSeconds = 60;

    private static Process? _serverProcess;
    public static bool IsServerReused { get; private set; }

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        EnsureLocalDbStarted();

        if (await IsPortOpenAsync(Port))
        {
            IsServerReused = true;
            TestContext.Progress.WriteLine("[GlobalAppHost] 既存サーバー検出。再利用モードで起動します。");
        }
        else
        {
            IsServerReused = false;
            _serverProcess = StartDotnetRun();
            await WaitForHttpReadyAsync(BaseUrl + "/Account/Login", TimeSpan.FromSeconds(ReadyTimeoutSeconds));
            TestContext.Progress.WriteLine("[GlobalAppHost] 子プロセスでアプリを起動しました。");
        }

        EnsurePlaywrightBrowsersInstalled();
        await DatabaseReset.RestoreSeedAsync();
        TestContext.Progress.WriteLine("[GlobalAppHost] テスト DB をシード状態へ復元しました。");
    }

    [OneTimeTearDown]
    public void Stop()
    {
        if (IsServerReused || _serverProcess is null)
        {
            return;
        }

        try
        {
            if (!_serverProcess.HasExited)
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(10_000);
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[GlobalAppHost] 子プロセス停止失敗: {ex.Message}");
        }
        finally
        {
            _serverProcess.Dispose();
            _serverProcess = null;
        }
    }

    private static void EnsureLocalDbStarted()
    {
        var info = RunSqlLocalDb($"info {LocalDbInstance}");
        if (IsLocalDbRunning(info))
        {
            return;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var start = RunSqlLocalDb($"start {LocalDbInstance}");
            var verify = RunSqlLocalDb($"info {LocalDbInstance}");
            if (verify.Contains("State: Running", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TestContext.Progress.WriteLine($"[GlobalAppHost] LocalDB 起動リトライ {attempt + 1}: {start}");
        }

        throw new InvalidOperationException(
            $"LocalDB '{LocalDbInstance}' を起動できませんでした。'sqllocaldb stop {LocalDbInstance}' を試した後に再実行してください。");
    }

    private static bool IsLocalDbRunning(string info)
    {
        // sqllocaldb info の出力は "State:    Running" のように複数スペースを含むため、
        // 行ごとに State: の値を抽出して Running かを判定する。
        foreach (var line in info.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("State:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed.Substring("State:".Length).Trim();
                return value.Equals("Running", StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    private static string RunSqlLocalDb(string args)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(15_000);
            return output + Environment.NewLine + error;
        }
        catch (Exception ex)
        {
            return $"sqllocaldb 呼び出し失敗: {ex.Message}";
        }
    }

    private static async Task<bool> IsPortOpenAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await client.ConnectAsync("localhost", port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static Process StartDotnetRun()
    {
        var workingDir = ResolveAppProjectDirectory();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --no-launch-profile --project \"{Path.Combine(workingDir, "EmployeeManagement.csproj")}\" --environment Test --urls {BaseUrl}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDir,
        };
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Test";

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.Progress.WriteLine($"[App] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.Progress.WriteLine($"[App-ERR] {e.Data}");
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private static string ResolveAppProjectDirectory()
    {
        // EmployeeManagement.E2ETests/bin/Debug/net8.0/ から 4 階層上が編集用アプリのフォルダ自身
        var here = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(here, "..", "..", "..", ".."));
        if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "EmployeeManagement.csproj")))
        {
            return candidate;
        }

        throw new DirectoryNotFoundException($"編集用アプリのフォルダ（EmployeeManagement.csproj）が見つかりません: {candidate}");
    }

    private static async Task WaitForHttpReadyAsync(string url, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(url);
                if ((int)response.StatusCode is >= 200 and < 500)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"アプリ起動を {timeout.TotalSeconds:F0} 秒以内に確認できませんでした。{(lastException is null ? string.Empty : "最終エラー: " + lastException.Message)}");
    }

    private static void EnsurePlaywrightBrowsersInstalled()
    {
        try
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
            if (exitCode != 0)
            {
                TestContext.Progress.WriteLine($"[GlobalAppHost] playwright install chromium が exitCode={exitCode} を返しました。");
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[GlobalAppHost] playwright install で例外: {ex.Message}");
        }
    }
}
