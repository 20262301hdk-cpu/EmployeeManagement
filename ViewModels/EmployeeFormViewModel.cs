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
    [EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません。")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードは必須です。")]
    [StringLength(100, MinimumLength = 8,ErrorMessage = "パスワードは8文字以上100文字以下で入力してください。")]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string? Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "確認用パスワードは必須です。")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "パスワードと確認用パスワードが一致しません。")]
    [Display(Name = "パスワード確認")]
    public string ConfirmPassword { get; set; } = string.Empty;


    [Display(Name = "氏名")]
    [Required(ErrorMessage ="氏名は必須です。")]
    [StringLength(30, ErrorMessage = "氏名は 30 文字以内で入力してください。")]
    public string EmpName { get; set; } = string.Empty;

    [Display(Name = "性別")]
    [Required(ErrorMessage = "性別は必須です。")]
    [Range(1, 2, ErrorMessage = "性別の選択値が不正です。")]
    public int Gender { get; set; } = 1;

    [Required(ErrorMessage = "住所は必ず入力してください。")]
    [StringLength(60, ErrorMessage = "住所は 60 文字以内で入力してください。")]
    [Display(Name = "住所")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "生年月日は必須です。")]
    [DataType(DataType.Date)]
    //[Range(typeof(DateTime), "1900/01/01", "2100/12/31", ErrorMessage = "生年月日は1900/01/01から2100/12/31の範囲で入力してください。")]
    [Display(Name = "生年月日")]
    public DateTime Birthday { get; set; }

    [Required(ErrorMessage ="ロールを選択してください。")]
    [Display(Name = "ロール")]
    [RegularExpression("^(Admin|User)$", ErrorMessage = "ロールの選択値が不正です。")]
    public string? Role { get; set; } = "User";

    [Required(ErrorMessage = "部署は必須です。")]
    [Range(1, int.MaxValue, ErrorMessage = "部署を選択してください。")]
    [Display(Name = "部署")]
    public int DeptId { get; set; }

    [JsonIgnore]
    public IEnumerable<SelectListItem> Departments { get; set; } 
            = Enumerable.Empty<SelectListItem>();

    public bool IsEdit { get; set; }
    public string DeptName { get; set; } = string.Empty;
}
