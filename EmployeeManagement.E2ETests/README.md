> 実装ステータス: 📌 配布済み（本プロジェクト全体が配布物。受講生は触らない）
> 受講生は TODO 実装後にテストを 実行するだけ。凡例: スタートガイド §1.1 を参照。

# EmployeeManagement.E2ETests

社員管理システム（機能削減版）の自動 E2E テスト。受講生は **Visual Studio で「すべてのテストを実行」を押すだけ** で動作確認できる。

> **配置（v1.2.0 以降）**: 本プロジェクトは編集用ソリューションに同梱されており、受講生が `C:\AspNet_Exercise\EmployeeManagement\` にコピーすると `C:\AspNet_Exercise\EmployeeManagement\EmployeeManagement.E2ETests\` にこの README が置かれた状態になる。アプリ本体 (`EmployeeManagement.csproj`) は親フォルダの `C:\AspNet_Exercise\EmployeeManagement\` 直下にある。受講生向けの実行手順は `../../../002_テスト/(PRGDE010)09_E2Eテスト実行ガイド_受講生向け_1.0.0.md` に集約しているため、本書は補助情報としてのみ参照すること。

## 1. 初回セットアップ (1 回だけ)

1. `EmployeeManagement.sln` を Visual Studio 2022 で開く
2. ソリューションのビルド (`Ctrl + Shift + B`)
3. PowerShell でブラウザ ドライバをインストール:

   ```powershell
   dotnet exec --runtimeconfig "EmployeeManagement.E2ETests/bin/Debug/net8.0/EmployeeManagement.E2ETests.runtimeconfig.json" `
               --depsfile     "EmployeeManagement.E2ETests/bin/Debug/net8.0/EmployeeManagement.E2ETests.deps.json" `
                              "EmployeeManagement.E2ETests/bin/Debug/net8.0/Microsoft.Playwright.dll" `
               install chromium
   ```

   PowerShell 7 (`pwsh`) があれば次でも可。

   ```powershell
   pwsh ./EmployeeManagement.E2ETests/bin/Debug/net8.0/playwright.ps1 install chromium
   ```

4. LocalDB が動いていることを確認:

   ```powershell
   sqllocaldb info MSSQLLocalDB
   ```

   `State: Running` でない場合は `sqllocaldb start MSSQLLocalDB`。

## 2. 通常実行

### Visual Studio (受講生既定)

- メニュー: `テスト` → `すべてのテストを実行`
- `.runsettings` 既定で `TestCategory=Smoke` が効き、🎯 課題範囲 5 件 (約 10 秒) を実行
- ⭐ チャレンジ範囲は `TestCategory=Challenge` で別途絞り込み (3 件)

### CLI

```powershell
dotnet test --filter TestCategory=Smoke      # 🎯 課題範囲 5 件 (受講生向け既定)
dotnet test --filter TestCategory=Challenge  # ⭐ チャレンジ範囲 3 件 (チャレンジ実装後に PASS)
```

## 3. テストカテゴリ

| カテゴリ | ケース数 | 用途 |
|---------|---------|------|
| Smoke     | 5 | 🎯 課題範囲のみで PASS する受講生既定セット。主要機能 (ログイン / 一覧 / 詳細 / 新規登録) の即時確認 |
| Challenge | 3 | ⭐ チャレンジ実装後に PASS する追加セット。部署検索 (T-12C) / 編集 (T-23) / 削除 (T-29) の確認 |

### 🎯 Smoke 5 ケース (課題範囲)

| ID  | 機能 | クラス |
|-----|------|--------|
| T01 | ログイン成功 (一般ユーザー) | LoginSmokeTests |
| T04 | ログイン失敗 (未登録 email) | LoginSmokeTests |
| T12 | 一覧緩い検証 (経理部の社員が含まれる) | ListSmokeTests |
| T14 | 詳細画面遷移 | ListSmokeTests |
| T16 | 新規登録 完了パス (3 ステップ) | CrudSmokeTests |

### ⭐ Challenge 3 ケース (チャレンジ範囲)

| ID   | 機能 | クラス | 必要な実装 |
|------|------|--------|------------|
| T12C | 部署検索 (経理部、厳密検証) | ListSmokeTests | T-06C / T-14C (部署DD) |
| T23  | 編集 完了パス (3 ステップ) | CrudSmokeTests | T-09C / T-13C / T-14C |
| T29  | 削除 完了パス (2 ステップ) | CrudSmokeTests | T-09C / T-14C |


## 4. スクリーンショット確認

- 出力先: `EmployeeManagement.E2ETests/bin/Debug/net8.0/screenshots/`
- 命名: `T-XX_stepN_label.png`
- 失敗時の Trace Viewer 用 zip: `bin/Debug/net8.0/traces/{TestFullName}.zip`
  - 表示: PowerShell から `dotnet exec ... Microsoft.Playwright.dll show-trace path/to/trace.zip`

## 5. 仕組み

- `Fixtures/GlobalAppHost.cs` (`[SetUpFixture]`): LocalDB 起動確認 → ポート 5050 probe → `dotnet run --environment Test --urls http://localhost:5050` を子プロセス起動 → `playwright install chromium` → `DatabaseReset.RestoreSeedAsync`
- `Fixtures/DatabaseReset.cs`: `EmployeeManagementDb_Test` に対し `DELETE FROM Employees / AspNetUserRoles / AspNetUsers / Departments` で完全リセット → `Program.SeedAsync` を呼んでシード再投入
- `Fixtures/AppPageTest.cs`: `PageTest` 派生。各テスト前に DB リセット + Tracing 開始、失敗時は `traces/*.zip` 出力
- `Helpers/LoginHelper.cs`: ログイン / ログアウト操作の共通ヘルパー
- `Helpers/SeedConstants.cs`: シードデータの定数定義

## 6. トラブルシュート

| 症状 | 対処 |
|------|------|
| ポート 5050 が使用中 | `Get-NetTCPConnection -LocalPort 5050 \| %{Stop-Process -Id $_.OwningProcess -Force}` |
| LocalDB `Cannot create automatic instance` | `sqllocaldb stop MSSQLLocalDB; sqllocaldb start MSSQLLocalDB` |
| ブラウザ未インストール | 上記セットアップ手順 3 を再実行 |
| DB が壊れた | `sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -Q "DROP DATABASE EmployeeManagementDb_Test"` 後にテスト再実行 (Fixture が自動で再作成) |
| Playwright の `Process exited` 失敗 | `Microsoft.Playwright` パッケージのバージョン (現在 1.55.0) を確認 |

## 7. 既知の制約

- **PowerShell 7 が無い場合**: Windows PowerShell 5.1 では `playwright.ps1` がアクセス違反で失敗するため、`dotnet exec` で直接 Microsoft.Playwright.dll を叩く必要がある (上記セットアップ参照)。
