using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EmployeeManagement.Models;

public class Employee
{
    public int EmpId { get; set; }

    [Required]
    public string AspNetUserId { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string EmpName { get; set; } = string.Empty;

    [Required, Range(1, 2)]
    public int Gender { get; set; }

    [Required, StringLength(60)]
    public string Address { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime Birthday { get; set; }

    [Required]
    public int DeptId { get; set; }

    public Department? Department { get; set; }
    public IdentityUser? AspNetUser { get; set; }
}
