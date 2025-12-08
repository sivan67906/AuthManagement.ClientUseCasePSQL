using System.ComponentModel.DataAnnotations;

namespace AuthManagement.Models;

public class RolePagePermissionMappingDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid PageId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class CreateRolePagePermissionMappingDto
{
    public Guid RoleId { get; set; }
    public Guid PageId { get; set; }
    public Guid PermissionId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateRolePagePermissionMappingDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PageId { get; set; }
    public Guid PermissionId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; }
}

public class RolePagePermissionMappingFormModel
{
    public string Id { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Role is required")]
    public string RoleId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Page is required")]
    public string PageId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Permission is required")]
    public string PermissionId { get; set; } = string.Empty;
    
    public string DepartmentId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
