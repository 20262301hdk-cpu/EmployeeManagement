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
        var query = _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .OrderBy(e => e.EmpId)
            .AsQueryable();

        // TODO: T-06 [課題] [API: Where / string.Contains]
        //        → 詳細設計書 §7.2 / §13.1
        if (!string.IsNullOrWhiteSpace(empName))
        {
            query = query.Where(e => e.EmpName.Contains(empName));
        }


        // TODO: T-06C [チャレンジ] [API: Where / int?.HasValue / int?.Value]
        //        → 詳細設計書 §7.2 / §13.2

        var rows = query.Select(e => new EmployeeRowViewModel
        {
            EmpId = e.EmpId,
            EmpName = e.EmpName,
            Gender = e.Gender,
            Birthday = e.Birthday,
            DeptName = e.Department != null ? e.Department.DeptName : string.Empty
        });

        // 暫定 (チャレンジ未実装時): 全件を 1 ページに格納して IPagedList<EmployeeRowViewModel> を成立させる。
        // TODO: T-06C [チャレンジ] [API: ToPagedListAsync] 下の 2 行を Items = await rows.ToPagedListAsync(page, PageSize); に置き換える
        //        → 詳細設計書 §7.2 / §13.2
        var list = await rows.ToListAsync();
        var items = list.ToPagedList(1, Math.Max(list.Count, 1));

        var vm = new EmployeeListViewModel
        {
            EmpName = empName,
            DeptId = deptId,
            Page = page,
            Departments = await BuildDepartmentSelectListAsync(includeEmpty: true, deptId),
            Items = items
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.AspNetUser)
            .FirstOrDefaultAsync(e => e.EmpId == id);
        if (employee is null || employee.AspNetUser is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(employee.AspNetUser);
        var vm = new EmployeeRowViewModel
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

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new EmployeeFormViewModel { Gender = 1, Role = "User", IsEdit = false };
        vm.Departments = await BuildDepartmentSelectListAsync(includeEmpty: false, vm.DeptId);
        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel vm, bool back = false)
    {
        // TODO: T-07 [課題] [API: TempData / ModelState.IsValid / RedirectToAction / BuildDepartmentSelectListAsync]
        //        → 詳細設計書 §7.2 / §13.1
        ValidateBirthdayRange(vm);
        if(back)
        {
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            return View(vm);

        }
        // 入力チェック
        ValidateCreatePassword(vm);

        ValidateBirthdayRange(vm);

        await ValidateEmailDuplicateAsync(vm.Email);
        // エラー時
        if (!ModelState.IsValid)
        {
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);

            return View(vm);
        }

        // 部署名設定
        await SetDepartmentNameAsync(vm);

        // TempData保存
        SaveFormToTempData(vm);

        // 確認画面へ
        return RedirectToAction(nameof(CreateConfirm));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult CreateConfirm()
    {
        var vm = ReadFormFromTempData(keep: true);
        if (vm is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComplete()
    {
        // TODO: T-08 [課題] [API: TempData / UserManager.CreateAsync / UserManager.AddToRoleAsync / Employees.Add / SaveChangesAsync / ViewBag]
        //        → 詳細設計書 §7.2 / §13.1
        // TempDataから復元
        var vm = ReadFormFromTempData(keep: false);

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

        var result = await _userManager.CreateAsync(user, vm.Password!);

        // ユーザー作成失敗時
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);

            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);

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

        _db.Employees.Add(employee);

        await _db.SaveChangesAsync();

        // 新規社員IDをViewBagへ
        ViewBag.EmpId = employee.EmpId;

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
        // 2. 戻るボタンの処理
        if (back)
        {
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            return View(vm);
        }

        // 3. その他のバリデーションチェック
        ValidateBirthdayRange(vm);
        await ValidateEmailDuplicateAsync(vm.Email, vm.EmpId);
        // 1. 【最優先】パスワードが空欄の場合のエラー消去処理
        // これをメソッドの先頭に持ってくることで、[Required] の初期エラーを完全にリセットします
        if (string.IsNullOrWhiteSpace(vm.Password))
        {
            // パスワードと確認用パスワードの Required / Compare エラーを強制削除
            ModelState.Remove(nameof(vm.Password));
            ModelState.Remove(nameof(vm.ConfirmPassword));

            vm.Password = string.Empty;
            vm.ConfirmPassword = string.Empty;
        }
        // パスワードが入力されている場合は、確認用パスワードとの一致チェックを行う
        else
        {
            if (string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.ConfirmPassword),
                    "確認用パスワードは必須です。");
            }
            else if (vm.Password != vm.ConfirmPassword)
            {
                ModelState.AddModelError(
                    nameof(vm.ConfirmPassword),
                    "パスワードと確認用パスワードが一致しません。");
            }
        }


        // 4. エラー判定（この段階ではパスワードの不要なエラーは消えています）
        if (!ModelState.IsValid)
        {
            vm.Departments = await BuildDepartmentSelectListAsync(false, vm.DeptId);
            return View(vm);
        }

        // 5. 部署名設定と確認画面への遷移処理
        await SetDepartmentNameAsync(vm);

        // TempData保存
        SaveFormToTempData(vm);

        // 確認画面へ
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
        return RedirectToAction(nameof(Index));
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
        // 新規登録時のみ
        if (!vm.IsEdit)
        {
            // パスワード必須
            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(
                    nameof(vm.Password),
                    ValidationMessages.Password_Required);
            }

            // 確認用必須
            if (string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.ConfirmPassword),
                    "確認用パスワードは必須です。");
            }
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
