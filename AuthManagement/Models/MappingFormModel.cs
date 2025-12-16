using System.ComponentModel.DataAnnotations;

namespace AuthManagement.Models;

public class MappingFormModel
{
    [Required(ErrorMessage = "Department is required")]
    public string DepartmentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "User is required")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public string RoleId { get; set; } = string.Empty;
}

public class RolePagePermissionFormModel
{
    public string RoleId { get; set; } = string.Empty;
    public string PageId { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public List<Guid> SelectedPermissionIds { get; set; } = new();
}
