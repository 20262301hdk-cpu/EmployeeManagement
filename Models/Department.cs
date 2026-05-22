using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Resources;

namespace EmployeeManagement.Models;

public class Department
{
    public int DeptId { get; set; }

    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.DeptName_Required))]
    [StringLength(15, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.DeptName_Length))]
    public string DeptName { get; set; } = string.Empty;

    public ICollection<Employee>? Employees { get; set; }
}
