using System.Text.Json;
using EmployeeManagement.Data;
using EmployeeManagement.Models;
using EmployeeManagement.Resources;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.EF;

namespace EmployeeManagement.Controllers;

[Authorize]
public class EmployeesController : Controller
{
    private const int PageSize = 10;
    private const string TempDataKey = "EmployeeForm";
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public EmployeesController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? empName, int? deptId, int page = 1)
    {
        // Employeesテーブルからデータ取得準備
        // 部署情報も一緒に取得し、社員ID順に並べる
        var query = _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .OrderBy(e => e.EmpId)
            .AsQueryable();

        // TODO: T-06 [課題] [API: Where / string.Contains]→ 詳細設計書 §7.2 / §13.1
        // 氏名検索
        // empNameが入力されている場合、名前に検索文字が含まれる社員だけ取得
        if (!string.IsNullOrWhiteSpace(empName))
        {
            query = query.Where(e => e.EmpName.Contains(empName));
        }
        // 部署検索
        // 部署IDが選択されている場合、該当部署の社員だけ取得
        if (deptId.HasValue)
        {
            query = query.Where(e => e.DeptId == deptId.Value);
        }

        // TODO: T-06C [チャレンジ] [API: Where / int?.HasValue / int?.Value]→ 詳細設計書 §7.2 / §13.2
        // Employee → EmployeeRowViewModel に変換
        var rows = query.Select(e => new EmployeeRowViewModel
        {
            EmpId = e.EmpId,
            EmpName = e.EmpName,
            Gender = e.Gender,
            Birthday = e.Birthday,

            // Departmentがnullの場合は空文字
            DeptName = e.Department != null ? e.Department.DeptName : string.Empty
        });

        // 暫定 (チャレンジ未実装時): 全件を 1 ページに格納して IPagedList<EmployeeRowViewModel> を成立させる。
        // TODO: T-06C [チャレンジ] [API: ToPagedListAsync]下の 2 行を
        // Items = await rows.ToPagedListAsync(page, PageSize); に置き換える→ 詳細設計書 §7.2 / §13.2
        // ページング処理
        // 1ページ10件でデータ取得
        var list = await rows.ToListAsync();
        var items = list.ToPagedList(page, PageSize);

        // Viewに渡すデータ作成
        var vm = new EmployeeListViewModel
        {
            // 検索条件保持
            EmpName = empName,
            DeptId = deptId,

            // 現在ページ
            Page = page,

            // 部署ドロップダウン作成
            Departments = await BuildDepartmentSelectListAsync(includeEmpty: true, deptId),

            // 一覧データ
            Items = items
        };
        //Viewへ返す
        return View(vm);
    }

    //社員詳細画面
    public async Task<IActionResult> Details(int id)
    {
        // 社員・部署情報・ログインユーザー情報取得
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.AspNetUser)
            .FirstOrDefaultAsync(e => e.EmpId == id);

        // 社員が存在しない場合
        if (employee is null || employee.AspNetUser is null)
        {
            return NotFound();
        }

        //ロール取得
        var roles = await _userManager.GetRolesAsync(employee.AspNetUser);

        // 画面表示用ViewModel作成
        var vm = new EmployeeRowViewModel
        {
            EmpId = employee.EmpId,
            EmpName = employee.EmpName,
            Email = employee.AspNetUser.Email ?? string.Empty,
            Gender = employee.Gender,
            Address = employee.Address,
            Birthday = employee.Birthday,

            // 最初のロールを表示
            Role = roles.FirstOrDefault() ?? string.Empty,
            // null対策
            DeptName = employee.Department?.DeptName ?? string.Empty
        };
        // 詳細画面へ渡す
        return View(vm);
    }

    // 新規登録画面表示(GET)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // 初期値設定
        var vm = new EmployeeFormViewModel 
        { Gender = 1, Role = "User", IsEdit = false };
        // 部署一覧取得
        vm.Departments = await BuildDepartmentSelectListAsync(includeEmpty: false, vm.DeptId);
        // 入力画面表示
        return View(vm);
    }

    // 新規登録処理(POST)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel vm, bool back = false)
    {
        // TODO: T-07 [課題] [API: TempData / ModelState.IsValid / RedirectToAction / BuildDepartmentSelectListAsync] → 詳細設計書 §7.2 / §13.1
        // 生年月日範囲チェック
        ValidateBirthdayRange(vm);

        // 確認画面から戻るボタン押下時
        if (back)
        {
            // 部署一覧再設定
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            // 入力画面へ戻る
            return View(vm);

        }

        // 入力チェック

        // パスワードチェック
        ValidateCreatePassword(vm);
        // 生年月日チェック
        ValidateBirthdayRange(vm);
        // メール重複チェック
        await ValidateEmailDuplicateAsync(vm.Email);
        // バリデーションエラー時
        if (!ModelState.IsValid)
        {
            // 部署一覧再設定
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);

            // 入力画面再表示
            return View(vm);
        }

        // 確認画面表示準備
        // 部署名設定
        await SetDepartmentNameAsync(vm);

        // TempData保存
        SaveFormToTempData(vm);

        // 確認画面へ
        return RedirectToAction(nameof(CreateConfirm));
    }

    // 新規登録確認画面
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult CreateConfirm()
    {
        // TempDataからデータ取得
        var vm = ReadFormFromTempData(keep: true);

        // データがない場合は一覧へ
        if (vm is null)
        {
            return RedirectToAction(nameof(Index));
        }

        // 確認画面表示
        return View(vm);
    }

    // 新規登録完了処理
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComplete()
    {
        // TODO: T-08 [課題] [API: TempData / UserManager.CreateAsync / UserManager.AddToRoleAsync
        // / Employees.Add / SaveChangesAsync / ViewBag]→ 詳細設計書 §7.2 / §13.1
        // TempDataから復元
        var vm = ReadFormFromTempData(keep: false);

        // データがない場合
        if (vm is null)
        {
            return RedirectToAction(nameof(Index));
        }

        // IdentityUser作成
        var user = new IdentityUser
        {
            UserName = vm.Email,
            Email = vm.Email
        };

        // ユーザー登録
        var result = await _userManager.CreateAsync(user, vm.Password!);

          // 登録失敗時
        if (!result.Succeeded)
        {
            // エラーメッセージ追加
            AddIdentityErrors(result);

            // 部署一覧再設定
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);

            // 入力画面へ戻る
            return View("Create", vm);
        }

        // ロール付与
        await _userManager.AddToRoleAsync(user, vm.Role!);

        // Employeeテーブル登録
        var employee = new Employee
        {
            AspNetUserId = user.Id,
            EmpName = vm.EmpName,
            Gender = vm.Gender,
            Address = vm.Address,
            Birthday = vm.Birthday,
            DeptId = vm.DeptId
        };

        // DB追加
        _db.Employees.Add(employee);

        // DB保存
        await _db.SaveChangesAsync();

        // 完了画面表示
        // 新規社員IDをViewBagへ
        ViewBag.EmpId = employee.EmpId;

        // 完了画面表示
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildEditFormAsync(id);
        if (vm is null)
        {
            return NotFound();
        }

        vm.Departments = await BuildDepartmentSelectListAsync(includeEmpty: false, vm.DeptId);
        return View(vm);
    }

    // TODO: T-09C [チャレンジ] [API: TempData / ModelState.IsValid / RedirectToAction / BuildDepartmentSelectListAsync]
    //        → 詳細設計書 §7.2 / §13.2
    // 暫定 (チャレンジ未実装時): 編集機能は未実装なので、一覧へ戻りつつ TempData にメッセージを残す
    //await Task.CompletedTask;
    //TempData["ChallengeMessage"] = "社員編集 (T-09C) はチャレンジ課題です。EmployeesController.cs を実装してください。";
    //return RedirectToAction(nameof(Index));
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel vm, bool back = false)
    {
        if (back)
        {
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            return View(vm);
        }

        // 編集時は、パスワードが空欄ならパスワード変更なし。
        // この場合、パスワード確認もチェックしない。
        ValidateEditPassword(vm);

        ValidateBirthdayRange(vm);
        await ValidateEmailDuplicateAsync(vm.Email, vm.EmpId);

        if (!ModelState.IsValid)
        {
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            return View(vm);
        }

        await SetDepartmentNameAsync(vm);
        SaveFormToTempData(vm);

        return RedirectToAction(nameof(EditConfirm), new { id = vm.EmpId });
    }


   

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult EditConfirm(int id)
    {
        var vm = ReadFormFromTempData(keep: true);
        if (vm is null || vm.EmpId != id)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComplete()
    {
        // TODO: T-09C [チャレンジ] [API: UserManager.UpdateAsync / GeneratePasswordResetTokenAsync / ResetPasswordAsync / RemoveFromRolesAsync / AddToRoleAsync / SaveChangesAsync]
        //        → 詳細設計書 §7.2 / §13.2
        // TempData から復元
        var vm = ReadFormFromTempData(keep: false);
        if (vm is null)
        {
            return RedirectToAction(nameof(Index));
        }
        // DBから既存の社員データを取得（関連するAspNetUserもIncludeする）
        var employee = await _db.Employees
            .Include(e => e.AspNetUser)
            .FirstOrDefaultAsync(e => e.EmpId == vm.EmpId);

        if (employee is null || employee.AspNetUser is null)
        {
            return RedirectToAction(nameof(Index));
        }

        // --- 1. IdentityUserの更新（メールアドレス） ---
        employee.AspNetUser.UserName = vm.Email;
        employee.AspNetUser.Email = vm.Email;
        var userResult = await _userManager.UpdateAsync(employee.AspNetUser);
        if (!userResult.Succeeded)
        {
            AddIdentityErrors(userResult);
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            return View("Edit", vm);
        }

        // --- 2. パスワードの変更（入力がある場合のみ） ---
        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            // 管理者権限で強制的にリセット・再設定するパターン
            var token = await _userManager.GeneratePasswordResetTokenAsync(employee.AspNetUser);
            var passResult = await _userManager.ResetPasswordAsync(employee.AspNetUser, token, vm.Password);
            if (!passResult.Succeeded)
            {
                AddIdentityErrors(passResult);
                vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
                return View("Edit", vm);
            }
        }

        // --- 3. ロールの更新 ---
        var currentRoles = await _userManager.GetRolesAsync(employee.AspNetUser);
        if (!currentRoles.Contains(vm.Role))
        {
            // 既存のロールを一度削除して、新しいロールを付与
            await _userManager.RemoveFromRolesAsync(employee.AspNetUser, currentRoles);
            await _userManager.AddToRoleAsync(employee.AspNetUser, vm.Role!);
        }

        // --- 4. Employeeテーブルの更新 ---
        employee.EmpName = vm.EmpName;
        employee.Gender = vm.Gender;
        employee.Address = vm.Address;
        employee.Birthday = vm.Birthday;
        employee.DeptId = vm.DeptId;

        await _db.SaveChangesAsync();

        // 編集された社員IDをViewBagへ渡して完了画面を表示
        ViewBag.EmpId = employee.EmpId;
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var vm = await BuildDetailsViewModelAsync(id);
        if (vm is null)
        {
            return NotFound();
        }

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComplete(int id)
    {
        // TODO: T-09C [チャレンジ] [API: Employees.Include / Employees.Remove / SaveChangesAsync / UserManager.DeleteAsync]
        //        → 詳細設計書 §7.2 / §13.2
        // 削除対象の社員データを取得
        var employee = await _db.Employees
         .Include(e => e.AspNetUser)
         .FirstOrDefaultAsync(e => e.EmpId == id);
        if (employee is null)
        {
            return NotFound();
        }
        // 先に Employee レコードを削除（外部キー制約の順序を考慮）
        var user = employee.AspNetUser;
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();

        // 次に IdentityUser を削除
        if (user is not null)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                // 必要に応じてエラー処理を追加
                ModelState.AddModelError(string.Empty, "ユーザーの削除に失敗しました。");
                return RedirectToAction(nameof(Index));
            }
        }
        // 削除が完了したら一覧画面へ遷移
        return View();
    }
    

    private async Task<EmployeeFormViewModel?> BuildEditFormAsync(int id)
    {
        var employee = await _db.Employees.Include(e => e.AspNetUser).FirstOrDefaultAsync(e => e.EmpId == id);
        if (employee is null || employee.AspNetUser is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(employee.AspNetUser);
        return new EmployeeFormViewModel
        {
            EmpId = employee.EmpId,
            Email = employee.AspNetUser.Email ?? string.Empty,
            EmpName = employee.EmpName,
            Gender = employee.Gender,
            Address = employee.Address,
            Birthday = employee.Birthday,
            Role = roles.FirstOrDefault() ?? "User",
            DeptId = employee.DeptId,
            IsEdit = true
        };
    }

    private async Task<EmployeeRowViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var employee = await _db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.AspNetUser)
            .FirstOrDefaultAsync(e => e.EmpId == id);
        if (employee is null || employee.AspNetUser is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(employee.AspNetUser);
        return new EmployeeRowViewModel
        {
            EmpId = employee.EmpId,
            EmpName = employee.EmpName,
            Email = employee.AspNetUser.Email ?? string.Empty,
            Gender = employee.Gender,
            Address = employee.Address,
            Birthday = employee.Birthday,
            Role = roles.FirstOrDefault() ?? string.Empty,
            DeptName = employee.Department?.DeptName ?? string.Empty
        };
    }

    private async Task<IEnumerable<SelectListItem>> BuildDepartmentSelectListAsync(bool includeEmpty, int? selectedValue)
    {
        var departments = await _db.Departments.AsNoTracking().OrderBy(d => d.DeptId).ToListAsync();
        var items = new List<SelectListItem>();
        if (includeEmpty)
        {
            items.Add(new SelectListItem("-----", string.Empty, !selectedValue.HasValue));
        }
        else
        {
            items.Add(new SelectListItem("選択してください", "0", selectedValue.GetValueOrDefault() == 0));
        }

        items.AddRange(departments.Select(d => new SelectListItem(d.DeptName, d.DeptId.ToString(), d.DeptId == selectedValue)));
        return items;
    }

    private async Task SetDepartmentNameAsync(EmployeeFormViewModel vm)
    {
        vm.DeptName = await _db.Departments
            .Where(d => d.DeptId == vm.DeptId)
            .Select(d => d.DeptName)
            .FirstOrDefaultAsync() ?? string.Empty;
    }

    private void SaveFormToTempData(EmployeeFormViewModel vm)
    {
        vm.Departments = Enumerable.Empty<SelectListItem>();
        TempData[TempDataKey] = JsonSerializer.Serialize(vm);
    }

    private EmployeeFormViewModel? ReadFormFromTempData(bool keep)
    {
        var json = keep ? TempData.Peek(TempDataKey) as string : TempData[TempDataKey] as string;
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<EmployeeFormViewModel>(json);
    }

    private void ValidateCreatePassword(EmployeeFormViewModel vm)
    {
        // 新規登録ではパスワード必須。
        ValidatePasswordWhenRequired(vm);
    }

    private void ValidateEditPassword(EmployeeFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Password))
        {
            // 編集時にパスワード未入力なら、パスワード変更なし。
            // 確認用パスワードに値が残っていても検証・更新しない。
            vm.Password = string.Empty;
            vm.ConfirmPassword = string.Empty;
            ModelState.Remove(nameof(vm.Password));
            ModelState.Remove(nameof(vm.ConfirmPassword));
            return;
        }

        // 編集時でもパスワードが入力された場合だけ、確認用パスワードとポリシーを検証する。
        ValidatePasswordWhenRequired(vm);
    }

    private void ValidatePasswordWhenRequired(EmployeeFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Password))
        {
            ModelState.AddModelError(nameof(vm.Password), ValidationMessages.Password_Required);
            return;
        }

        if (vm.Password.Length < 8 || vm.Password.Length > 100)
        {
            ModelState.AddModelError(nameof(vm.Password), "パスワードは8文字以上100文字以下で入力してください。");
        }

        if (string.IsNullOrWhiteSpace(vm.ConfirmPassword))
        {
            ModelState.AddModelError(nameof(vm.ConfirmPassword), "確認用パスワードは必須です。");
        }
        else if (vm.Password != vm.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(vm.ConfirmPassword), "パスワードと確認用パスワードが一致しません。");
        }
    }

    private async Task ValidateEmailDuplicateAsync(string email, int? currentEmpId = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return;
        }

        if (currentEmpId.HasValue)
        {
            var current = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmpId == currentEmpId.Value);
            if (current is not null && current.AspNetUserId == user.Id)
            {
                return;
            }
        }

        ModelState.AddModelError(nameof(EmployeeFormViewModel.Email), ValidationMessages.Email_Duplicate);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var message = error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                ? ValidationMessages.Password_Policy
                : error.Description;
            ModelState.AddModelError(string.Empty, message);
        }
    }

    // 事前実装 (削減版): Birthday の範囲チェック。ViewModel に [Range] 属性を付けるとカルチャー差異の影響を受けるため、Controller 側で集約する。
    private void ValidateBirthdayRange(EmployeeFormViewModel vm)
    {
        var min = new DateTime(1900, 1, 1);
        var max = new DateTime(2100, 12, 31);
        if (vm.Birthday < min || vm.Birthday > max)
        {
            ModelState.AddModelError(nameof(vm.Birthday), ValidationMessages.Birthday_Range);
        }
    }
}
