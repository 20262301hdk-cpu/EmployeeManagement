using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Resources;

namespace EmployeeManagement.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.Email_Required))]
    [EmailAddress(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.Email_Format))]
    [Display(Name = "メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.Password_Required))]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "ログイン状態を保持する")]
    public bool RememberMe { get; set; }
}
