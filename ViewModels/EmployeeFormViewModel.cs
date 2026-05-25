using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace EmployeeManagement.ViewModels;

// TODO: T-07 / T-13 [課題] F-11 入力チェック [API: [Required] / [StringLength] / [EmailAddress] / [Compare] / [Range] / [RegularExpression] / [DataType] / ErrorMessageResourceType / ErrorMessageResourceName]
//        → 詳細設計書 §6.4 / §8 / §13.1
public class EmployeeFormViewModel
{
    public int? EmpId { get; set; }

    [Required(ErrorMessage = "メールアドレスは必須です。")]
    [StringLength(256)]
    [Display(Name = "メール")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードは必ず入力してください。")]
    [StringLength(100)]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string? Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードと確認用パスワードが一致しません")]
    [StringLength(100)]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "パスワード確認")]
    public string ConfirmPassword { get; set; } = string.Empty;


    [Display(Name = "氏名")]
    [Required(ErrorMessage ="氏名は必須です。")]
    [StringLength(30)]
    public string EmpName { get; set; } = string.Empty;

    [Display(Name = "性別")]
    [Required(ErrorMessage = "性別を選択してください")]
    [Range(1, 2)]
    public int Gender { get; set; } = 1;

    [Required(ErrorMessage = "住所は必ず入力してください。")]
    [StringLength(60)]
    [Display(Name = "住所")]
    public string Address { get; set; } = string.Empty;

    [Required, DataType(DataType.Date, ErrorMessage = "生年月日は必須です。")]
    [Display(Name = "生年月日")]
    public DateTime Birthday { get; set; }

    [Required]
    [Display(Name = "ロール")]
    public string? Role { get; set; } = "User";

    [Required(ErrorMessage = "部署を選択してください。")]
    [Display(Name = "部署")]
    public int DeptId { get; set; }

    [JsonIgnore]
    public IEnumerable<SelectListItem> Departments { get; set; } 
            = Enumerable.Empty<SelectListItem>();

    public bool IsEdit { get; set; }
    public string DeptName { get; set; } = string.Empty;
}
