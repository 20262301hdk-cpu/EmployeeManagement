namespace EmployeeManagement.ViewModels;

public class EmployeeRowViewModel
{
    public int EmpId { get; set; }
    public string EmpName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string GenderText => Gender == 1 ? "男" : "女";
    public DateTime Birthday { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
