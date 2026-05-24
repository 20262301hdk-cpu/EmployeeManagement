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
        await SetDepartmentNameAsync(vm);

        return View("CreateConfirm", vm);
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
        await Task.CompletedTask;
        throw new NotImplementedException();
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

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel vm, bool back = false)
    {
        // TODO: T-09C [チャレンジ] [API: TempData / ModelState.IsValid / RedirectToAction / BuildDepartmentSelectListAsync]
        //        → 詳細設計書 §7.2 / §13.2
        // 暫定 (チャレンジ未実装時): 編集機能は未実装なので、一覧へ戻りつつ TempData にメッセージを残す
        await Task.CompletedTask;
        TempData["ChallengeMessage"] = "社員編集 (T-09C) はチャレンジ課題です。EmployeesController.cs を実装してください。";
        return RedirectToAction(nameof(Index));
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
        // 暫定 (チャレンジ未実装時): 一覧へ戻りつつ TempData にメッセージを残す
        await Task.CompletedTask;
        TempData["ChallengeMessage"] = "社員編集の完了処理 (T-09C) はチャレンジ課題です。EmployeesController.cs を実装してください。";
        return RedirectToAction(nameof(Index));
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
        // 暫定 (チャレンジ未実装時): 一覧へ戻りつつ TempData にメッセージを残す
        await Task.CompletedTask;
        TempData["ChallengeMessage"] = "社員削除 (T-09C) はチャレンジ課題です。EmployeesController.cs を実装してください。";
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
        if (string.IsNullOrWhiteSpace(vm.Password))
        {
            ModelState.AddModelError(nameof(vm.Password), ValidationMessages.Password_Required);
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
