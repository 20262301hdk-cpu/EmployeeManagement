using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EmployeeManagement.ViewModels;

// TODO: T-07 / T-13 [課題] F-11 入力チェック [API: [Required] / [StringLength] / [EmailAddress] / [Compare] / [Range] / [RegularExpression] / [DataType] / ErrorMessageResourceType / ErrorMessageResourceName]
//        → 詳細設計書 §6.4 / §8 / §13.1
public class EmployeeFormViewModel
{
    public int? EmpId { get; set; }

    [Display(Name = "メール")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "パスワード確認")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "氏名")]
    public string EmpName { get; set; } = string.Empty;

    [Display(Name = "性別")]
    public int Gender { get; set; } = 1;

    [Display(Name = "住所")]
    public string Address { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "生年月日")]
    public DateTime Birthday { get; set; }

    [Display(Name = "ロール")]
    public string Role { get; set; } = "User";

    [Display(Name = "部署")]
    public int DeptId { get; set; }

    [JsonIgnore]
    public IEnumerable<SelectListItem> Departments { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool IsEdit { get; set; }
    public string DeptName { get; set; } = string.Empty;
}
