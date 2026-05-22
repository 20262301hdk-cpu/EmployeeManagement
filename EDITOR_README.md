# 編集用プロジェクト (機能削減版)

`// TODO: T-XX [課題/チャレンジ] [API: ...]` / `<!-- TODO: T-XX [課題] -->` / `@* TODO: T-XXC [チャレンジ] [Tag: ...] *@` の形式で要点を空欄化したプロジェクト。**TODO 本文は使用 API / タグヘルパー名と詳細設計書への参照のみのシンタックス備忘**で、実装手順は **詳細設計書 `(PRGDE010)06_詳細設計書_1.0.0.md` §7.2 / §12.2 / §6.4 / §8 / §9.1** を正本とする。§13.1 / §13.2 は索引（T-番号 → 該当章節）。

> **配置先**: 本プロジェクトフォルダは受講生が **`C:\AspNet_Exercise\EmployeeManagement\` にコピーしてから作業する**前提に統一されている。コピー手順および提出（ZIP アップロード）の手順はスタートガイド `../../00_スタートガイド_受講生向け.md` §0.5 / §3 フェーズ E を参照。
>
> **E2E テスト同梱**: 同階層に `EmployeeManagement.E2ETests/` フォルダがあり、`EmployeeManagement.sln` から両プロジェクトを参照する。E2E テストの実行手順は `../../../002_テスト/(PRGDE010)09_E2Eテスト実行ガイド_受講生向け_1.0.0.md` を参照。

**本版は機能削減版です。** T-03 / T-05 / T-10 / T-11 は事前実装済みで、受講生が実装するのは **🎯 課題 (必須)** と **⭐ チャレンジ課題 (任意)** の 2 段階構成です。

| 区分 | 凡例 | 内容 | 未実装時の挙動 |
|------|------|------|--------------|
| 🎯 課題 | `T-06` / `T-07` / `T-08` / `T-13` / `T-14` (接尾辞なし) | F-03 一覧表示 / F-04 氏名検索 / F-07 社員登録 / F-11 入力チェック / F-14 ログインユーザ名表示 | 未実装だと 500 エラーやフォーム未描画 |
| ⭐ チャレンジ | `T-06C` / `T-09C` / `T-13C` / `T-14C` (接尾辞 `-C`) | F-05 部署検索 / F-08 社員編集 / F-09 社員削除 / F-13 ユーザ権限判定 | 未実装でもアプリは動作する (UI が表示されないだけ) |
| 🔒 事前実装 | `Details.cshtml` / F-06 社員詳細表示 ほか | F-01 ログイン / F-02 ログアウト / F-06 社員詳細表示 / F-10 アクセス拒否 / F-12 ログイン状態判定 (認証分岐) | 受講生は読むだけ |

> 事前実装箇所はコード内の `// 事前実装 (削減版)` コメントで識別できます。チャレンジ未実装の Edit/Delete アクションは TempData にメッセージを残して一覧へ Redirect する暫定実装になっています。F-11 入力チェックは配布物に DataAnnotations 属性も `EmployeeFormViewModel` 用の `ValidationMessages.resx` エントリも含まれないため、T-07/T-13 (🎯 課題) 実装時にまとめて追加します。F-14 ログインユーザ名表示は `_LoginPartial.cshtml` の `<span>@User.Identity.Name</span>` 行を T-14 (🎯 課題) 実装で有効化します。

## 0. マイグレーション関連機能は事前実装済み

本版では DB 構築に関わる以下を事前提供しているため、受講生は実装不要。すべて `// 事前実装 (削減版)` コメントで識別できる。

| 提供物 | ファイル | 役割 |
|--------|---------|------|
| Initial マイグレーション | `Migrations/20260510065105_InitialCreate.cs` / `*.Designer.cs` | AspNetUsers / Departments / Employees ほかのスキーマ生成 |
| モデルスナップショット | `Migrations/ApplicationDbContextModelSnapshot.cs` | 現行モデルのスナップショット |
| 自動マイグレーション呼び出し | `Program.cs` / `Program.Seed.cs` の `await db.Database.MigrateAsync` | アプリ起動時に未適用マイグレーションを自動適用 |
| シードデータ投入 | `Program.Seed.cs` `SeedAsync` / `CreateUserAndEmployeeAsync` (T-11) | 初回起動時に Role / Department / User / Employee を投入 |

> 受講生は `Migrations/` 配下と `Program.cs` / `Program.Seed.cs` を **編集してはならない** (§5 参照)。受講生が実行する操作は §2 PMC で DB を作成・更新する の手順のみ。
> アプリを `F5` / `Ctrl + F5` で起動するだけでも `MigrateAsync` が走るため DB は自動的に作成される。PMC からの `Update-Database` は明示的に DB を構築・確認したい場合の手順として案内する。

## 1. 初回起動手順 (Visual Studio)

詳細は説明資料 `(PRGDE010)01_説明資料_1.0.0.md` §3.4 を参照する。ここでは最短手順だけを示す。

| # | 手順 | 補足 |
|---|------|------|
| 1 | `EmployeeManagement.sln` を Visual Studio で開く | `IIS Express` ではなく `EmployeeManagement` プロファイルを使う |
| 2 | `NuGet パッケージの復元` を実行 | CLI なら `dotnet restore` |
| 3 | `Ctrl + Shift + B` でビルド | TODO 未実装段階ではエラーが残る場合がある |
| 4 | PMC で `Update-Database` を実行 | 詳細は §2 |
| 5 | `Ctrl + F5` で起動し `http://localhost:5050/` を開く | 起動時に Identity ロール / ユーザー / Employee が `SeedAsync` から投入される (Department は §2 の `Update-Database` で投入済み) |

> AccountController.cs は T-05 事前実装済みのためログイン関連コンパイルエラーは出ない。
## 2. PMC (パッケージ マネージャー コンソール) で DB を作成・更新する

> Visual Studio 2022 同梱の **PMC** から EF Core マイグレーションを実行する手順を、起動から検証まで通しでまとめる。CLI (`dotnet ef`) を使う場合は §1.2 / §4 の右列「同等コマンド」を参照。

### 2.1 PMC を開く

| # | 操作 | 説明 |
|---|------|------|
| 1 | メニュー `ツール` → `NuGet パッケージ マネージャー` → `パッケージ マネージャー コンソール` をクリック | Visual Studio 下部に `パッケージ マネージャー コンソール` ペインが出現する。すでにペインがある場合は表示するだけで良い。 |
| 2 | ペイン上部の `既定のプロジェクト` ドロップダウンで `EmployeeManagement` を選択 | これを忘れると `No DbContext was found` エラーが出る。ソリューション内のプロジェクトが 1 つしかないので最初から選択されているはずだが、必ず目視確認する。 |
| 3 | プロンプトが `PM>` になっていることを確認 | この状態で EF Core 用の PowerShell コマンドレット (`Update-Database` / `Add-Migration` / `Drop-Database` / `Get-Migration` / `Script-Migration`) が使える。 |

### 2.2 主要コマンドと期待される出力

`Update-Database` がメイン。それ以外は補助。**`Add-Migration` は受講生は使わない**（マイグレーションは事前提供されているため）。

| コマンド | 用途 | 期待される代表的な出力 |
|---------|------|--------------------|
| `Update-Database` | 未適用マイグレーションを LocalDB に適用 (DB が無ければ作成) | `Build started...` → `Build succeeded.` → `Applying migration '20260510065105_InitialCreate'.` → `Applying migration '20260516054331_SeedDepartments'.` → `Done.` |
| `Get-Migration` | プロジェクト内のマイグレーション一覧を確認 | `20260510065105_InitialCreate` と `20260516054331_SeedDepartments` の 2 行 (`Applied` 列が `True` ならば適用済み) |
| `Drop-Database -Force` | `EmployeeManagementDb` を削除 (テスト DB は対象外) | `Dropping database 'EmployeeManagementDb' on server '(localdb)\MSSQLLocalDB'.` → `Done.` |
| `Script-Migration` | 適用 SQL を確認用に出力 | `CREATE TABLE [Departments] ... CREATE TABLE [Employees] ...` の DDL がコンソールに流れる |

> 出力に `Build started...` `Build succeeded.` が含まれない場合は PMC が古い csproj をキャッシュしている。一度 `Ctrl + Shift + B` で全体ビルドしてから再実行する。

### 2.3 よくあるエラーと対処

| エラーメッセージ (抜粋) | 原因 | 対処 |
|-----------------------|------|------|
| `Your startup project 'EmployeeManagement' doesn't reference Microsoft.EntityFrameworkCore.Design.` | NuGet パッケージ復元未完了 | ソリューション右クリック → `NuGet パッケージの復元` → 再度 `Update-Database` |
| `No DbContext was found in assembly 'EmployeeManagement'.` | 既定のプロジェクト未選択 / TODO 未実装でビルド失敗中 | `既定のプロジェクト` を `EmployeeManagement` に切替。ビルドが通っていない場合は §1.2 の手順 4 (`Ctrl + Shift + B`) を先に成功させる |
| `The term 'Update-Database' is not recognized` | `Microsoft.EntityFrameworkCore.Tools` の PowerShell モジュールが読み込まれていない | Visual Studio を再起動し、`ツール` メニューから PMC を開き直す |
| `A network-related or instance-specific error occurred while establishing a connection... (provider: SQL Network Interfaces)` | LocalDB が停止中 | PowerShell で `sqllocaldb start MSSQLLocalDB`。説明資料 §3.4.2 / SQL 文 §2.2 を参照 |
| `Cannot drop database "EmployeeManagementDb" because it is currently in use.` | アプリがまだ動いている / SQL Server オブジェクト エクスプローラーで開いたままのクエリウィンドウがある | アプリを `Shift + F5` で停止、SSOX のクエリウィンドウを閉じてから再度 `Drop-Database -Force` |
| `The migration '20260510065105_InitialCreate' has already been applied to the database.` | 何度も `Update-Database` を実行した | **エラーではない**。すでに適用済みである旨の通知。DB を作り直したいときだけ `Drop-Database -Force` → `Update-Database` |

### 2.4 SQL Server オブジェクト エクスプローラー (SSOX) で検証する

`Update-Database` 完了後、テーブルが揃っていることを目視確認する。

| # | 操作 | 確認内容 |
|---|------|---------|
| 1 | メニュー `表示` → `SQL Server オブジェクト エクスプローラー` を開く | 左ペインに `SQL Server` ノード |
| 2 | `(localdb)\MSSQLLocalDB` → `データベース` を展開、`EmployeeManagementDb` ノードを右クリック → `更新` | DB 一覧が最新化される (`Update-Database` 実行直後でないと表示遅延あり) |
| 3 | `EmployeeManagementDb` → `テーブル` を展開 | `dbo.AspNetRoles` / `dbo.AspNetRoleClaims` / `dbo.AspNetUsers` / `dbo.AspNetUserRoles` / `dbo.AspNetUserClaims` / `dbo.AspNetUserLogins` / `dbo.AspNetUserTokens` / `dbo.Departments` / `dbo.Employees` / `dbo.__EFMigrationsHistory` の 10 テーブルが並ぶ |
| 4 | `dbo.__EFMigrationsHistory` を右クリック → `データの表示` | `MigrationId = 20260510065105_InitialCreate` と `20260516054331_SeedDepartments` の 2 行が入っている (適用済みであることの証跡) |
| 5 | `dbo.Departments` → `データの表示` | `Update-Database` 実行直後から `営業部` / `経理部` / `総務部` の 3 行が入っている (Migration の HasData による) |

> Department は `Update-Database` 完了時点で既に 3 件投入されている。Identity ロール / ユーザー / Employee はアプリを `F5` / `Ctrl + F5` で 1 度起動するとシードされる。

### 2.5 DB を初期化したいとき

| # | コマンド | 動作 |
|---|---------|------|
| 1 | `Drop-Database -Force` | `EmployeeManagementDb` を削除 |
| 2 | `Update-Database` | 空の DB を再作成、マイグレーション適用 (Department 3 件は HasData から投入される) |
| 3 | (Visual Studio で `Ctrl + F5`) | 起動時に `SeedAsync` が Identity ロール / ユーザー / Employee を再投入 |

> テスト DB (`EmployeeManagementDb_Test`) は E2E テストの `Fixtures/DatabaseReset.cs` が毎テスト前に自動でクリーンするため通常は手動操作不要。

## 3. 受講生環境で変更が必要になる場所

ポート競合や DB 命名規則の都合で値を変える場合、**変更箇所をすべて同じ値に揃える** こと。1 箇所だけ書き換えると編集用とテストで設定が乖離して動かなくなる。

### 3.1 起動ポート (既定 `5050`)

| # | ファイル | 該当キー |
|---|---------|---------|
| 1 | `Properties/launchSettings.json` | `profiles.EmployeeManagement.applicationUrl` |
| 2 | `EmployeeManagement.E2ETests/Fixtures/GlobalAppHost.cs` (編集用同梱) | `BaseUrl = "http://localhost:5050"` および `Port = 5050` |

### 3.2 LocalDB 接続文字列

| # | ファイル | 該当キー | DB 名 |
|---|---------|---------|------|
| 1 | `appsettings.json` | `ConnectionStrings.DefaultConnection` | `EmployeeManagementDb` |
| 2 | `appsettings.Test.json` | 同上 | `EmployeeManagementDb_Test` |
| 3 | `EmployeeManagement.E2ETests/appsettings.Test.json` (編集用同梱) | 同上 | `EmployeeManagementDb_Test` |

### 3.3 配布物の配置を変更した場合

E2E は編集用ソリューションに同梱されている (`EmployeeManagement.E2ETests/`)。配布物を別構造へ再配置したら、`GlobalAppHost.cs` の `ResolveAppProjectDirectory` と上記の相対パスを必ず再計算する。

## 4. TODO 一覧

### 4.1 受講生実装

#### 4.1.1 🎯 課題 (必須)

未実装だと一覧/登録/詳細が動作しません。最初に着手してください。

| TODO | ファイル | 箇所 | 内容の概要 |
|------|---------|----:|-----------|
| T-06 | `Controllers/EmployeesController.cs` | 1 | `Index`: `empName` 部分一致の `Where` を追加する (F-04 氏名検索) |
| T-07 | `Controllers/EmployeesController.cs` | 1 | `Create(POST)`: 入力検証 (`ModelState.IsValid` 分岐) + `TempData` 保存 + Redirect (F-07 / F-11)。`EmployeeFormViewModel.cs` の DataAnnotations と `ValidationMessages.resx` の EmployeeForm 用エントリも併せて付与 |
| T-08 | `Controllers/EmployeesController.cs` | 1 | `CreateComplete`: `UserManager` + `Employee` 追加 (F-07) |
| T-13 | `Views/Employees/Create.cshtml` | 1 | 新規登録の入力フォーム View + `asp-validation-summary` / `asp-validation-for` (F-07 / F-11) |
| T-14 | `Views/Employees/Index.cshtml` / `CreateConfirm.cshtml` / `CreateComplete.cshtml` / `Views/Shared/_LoginPartial.cshtml` (ユーザ名表示行有効化) | 4 | 一覧基本表示 (氏名検索フォーム+テーブル+詳細リンク) / 登録確認 / 登録完了 + ログイン中ユーザ名表示 (F-03 / F-07 / F-14)。`Details.cshtml` は §4.2 事前実装枠 |

#### 4.1.2 ⭐ チャレンジ課題 (任意)

未実装でもアプリは動作します (該当 UI が非表示)。課題が一通り完成してから着手してください。

| TODO | ファイル | 箇所 | 内容の概要 |
|------|---------|----:|-----------|
| T-06C | `Controllers/EmployeesController.cs` | 1 | `Index`: `deptId` 完全一致の `Where` 追加 + `ToPagedListAsync` ページング (F-05 / ページング) |
| T-09C | `Controllers/EmployeesController.cs` | 3 | `Edit(POST)` / `EditComplete` / `DeleteComplete` (F-08 / F-09) |
| T-13C | `Views/Employees/Edit.cshtml` | 1 | 編集の入力フォーム View + `asp-validation-summary` / `asp-validation-for` (F-08 / F-11) |
| T-14C | `Views/Employees/Index.cshtml` / `EditConfirm.cshtml` / `EditComplete.cshtml` / `Delete.cshtml` / `DeleteComplete.cshtml` / `Shared/_LoginPartial.cshtml` ([Admin] バッジ) | 6 | 一覧の部署DD/ページャ/編集削除リンク / 新規登録リンク / Admin バッジ / 編集削除画面群 (F-05 / F-08 / F-09 / F-13) |

> チャレンジ部分は Razor コメント `@* ... *@` でコメントアウトされています。受講生は該当ブロックを解除し、内側の TODO に従って実装してください。

### 4.2 事前実装 (削減版)

| TODO | ファイル | 内容の概要 | 状態 |
|------|---------|-----------|------|
| T-03 | `Data/ApplicationDbContext.cs` | `OnModelCreating` で Employee/Department のリレーション設定 | 🔒 事前実装 (削減版) |
| T-05 | `Controllers/AccountController.cs` | `Login(POST)` で SignInManager 認証、`Logout` で SignOut | 🔒 事前実装 (削減版) |
| T-10 | `Filter/LoggingActionFilter.cs` | `OnActionExecuting/Executed` で `_logger.LogInformation` | 🔒 事前実装 (削減版) |
| T-11 | `Program.cs` / `Program.Seed.cs` | `SeedAsync` と `CreateUserAndEmployeeAsync` の本体 | 🔒 事前実装 (削減版) |
| F-12 | `Controllers/*` の `[Authorize]` 属性 + `Views/Shared/_LoginPartial.cshtml` の認証分岐 (ログイン/ログアウトボタンの出し分け) | ログイン状態判定 (未認証ユーザーは Login へリダイレクト)。ログイン中ユーザ名表示は F-14 🎯 課題 (T-14 に内包) として切り出し済み | 🔒 事前実装 (削減版) |
| (M-00) | `Migrations/*.cs` (InitialCreate / SeedDepartments) | EF Core マイグレーション本体 + Department HasData (§0 参照、手動 `Update-Database` 適用) | 🔒 事前実装 (削減版・編集不可) |
| (T-14 一部) | `Views/Employees/Details.cshtml` | 社員詳細表示 View (F-06)。Action `Details(int id)` も事前実装 | 🔒 事前実装 (削減版) |

## 5. 編集してはいけないファイル

`Views/Employees/**/*.cshtml` は 🎯 課題 T-13 / T-14 と ⭐ チャレンジ T-13C / T-14C として受講生実装対象である (ただし `Views/Employees/Details.cshtml` は機能削減版で事前実装済みのため対象外)。以下は事前実装済みまたは触る必要のないファイルなので **書き換えないこと**。ただし 🎯 課題で例外あり: T-07/T-13 (F-11 入力チェック) で `Resources/ValidationMessages.resx` と `ViewModels/EmployeeFormViewModel.cs` に追記、T-14 (F-14 ログインユーザ名表示) で `Views/Shared/_LoginPartial.cshtml` のユーザ名表示行を有効化、⭐ T-14C (F-13 Admin バッジ) でも `Views/Shared/_LoginPartial.cshtml` を編集する。

| 区分 | ファイル | 理由 |
|------|---------|------|
| マイグレーション | `Migrations/*.cs` | EF Core の DB 構築に必要。事前実装済み (§0 参照) |
| 起動・シード | `Program.cs` / `Program.Seed.cs` | T-11 事前実装済み |
| DbContext | `Data/ApplicationDbContext.cs` | T-03 事前実装済み |
| アクションフィルター | `Filter/LoggingActionFilter.cs` | T-10 事前実装済み |
| 認証 | `Controllers/AccountController.cs` | T-05 事前実装済み |
| 社員詳細 View + Action (削減版固有) | `Views/Employees/Details.cshtml` + `EmployeesController.Details(int id)` | F-06 として事前実装済み (T-14 から切り離し) |
| リソース | `Resources/ValidationMessages.resx` | F-01 ログイン用エントリのみ配布 (※ F-11 課題で T-07/T-13 の一環として EmployeeForm 用エントリを追記) |
| 共通ビュー | `Views/Shared/**` (※ `_LoginPartial.cshtml` は T-14 / T-14C で編集可) / `Views/Account/**` / `Views/Home/**` / `Views/_*.cshtml` | 共通レイアウト・認証・ホーム画面 |
| エンティティ | `Models/*.cs` | プロパティ定義 |
| ViewModel | `ViewModels/*.cs` | (※ `EmployeeFormViewModel.cs` は F-11 課題で T-07/T-13 の一環として DataAnnotations 属性を付与可。`LoginViewModel.cs` の検証属性は F-01 事前実装) |

## 6. 動作確認のおすすめ順

### 6.1 🎯 課題 (必須)

1. T-13 (Create.cshtml) → 新規登録の入力フォームの入力欄と検証メッセージが表示される
2. T-14 (Index/CreateConfirm/CreateComplete) → 一覧 / 登録確認 / 登録完了画面が描画される (詳細画面は `Details.cshtml` が事前実装で配布時から動作)
3. T-06 → 一覧画面で氏名検索が動作する (部署検索/ページングはチャレンジ範囲)
4. T-07 / T-08 → 新規登録ができる

### 6.2 ⭐ チャレンジ課題 (任意)

5. T-13C (Edit.cshtml) → 編集画面の入力フォームを解除して実装
6. T-14C (Index 部署DD/ページャ/編集削除リンク / 新規登録リンク / Admin バッジ / EditConfirm/EditComplete/Delete/DeleteComplete) → コメントアウト解除して各画面を実装
7. T-06C → 部署検索 + ページング (`ToPagedListAsync`) を実装
8. T-09C → 編集 / 削除の Controller アクションを実装

## 7. 動作確認後 — Playwright (任意)

- E2E テストは編集用ソリューションに同梱されている (`EmployeeManagement.E2ETests/`)。受講生は自分で編集した編集用プロジェクトに対してそのまま動かせる。
- 初回のみ `dotnet exec ... Microsoft.Playwright.dll install chromium` でブラウザドライバを入れる。
- Smoke 5 件 (課題範囲) と Challenge 2 件 (チャレンジ範囲) の 2 段階構成。
  - `dotnet test EmployeeManagement.E2ETests --filter "TestCategory=Smoke"` → 課題のみ実装した状態で全 PASS する 5 件 (T-01 / T-04 / T-12 / T-14 / T-16)
  - `dotnet test EmployeeManagement.E2ETests --filter "TestCategory=Challenge"` → チャレンジ実装後に PASS する 2 件 (T-12C / T-23 / T-29)
- Visual Studio のテスト エクスプローラーから実行する場合は、カテゴリフィルタを `TestCategory=Smoke` (もしくは `TestCategory=Challenge`) で絞る。
- 失敗時は `bin/Debug/net8.0/screenshots/` のキャプチャと `bin/Debug/net8.0/traces/{TestFullName}.zip` を確認する。
- ポートや DB 名を変更した場合は §3 の表に従ってすべて揃える。
