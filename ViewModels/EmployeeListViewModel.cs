using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;

namespace EmployeeManagement.ViewModels;

public class EmployeeListViewModel
{
    [Display(Name = "氏名")]
    public string? EmpName { get; set; }
    [Display(Name = "部署")]
    public int? DeptId { get; set; }
    public int Page { get; set; } = 1;
    public IEnumerable<SelectListItem> Departments { get; set; } = Enumerable.Empty<SelectListItem>();
    public IPagedList<EmployeeRowViewModel> Items { get; set; } = new StaticPagedList<EmployeeRowViewModel>([], 1, 10, 0);
}
