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

/// <summary>
/// DTO for batch creating multiple permission mappings
/// </summary>
public class BatchCreateRolePagePermissionMappingDto
{
    public Guid RoleId { get; set; }
    public Guid PageId { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
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
    
    // For edit mode - single permission
    public string PermissionId { get; set; } = string.Empty;
    
    // For create mode - multiple permissions
    public List<string> SelectedPermissionIds { get; set; } = new();
    
    public string DepartmentId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Model for permission checkbox selection with View flag
/// </summary>
public class PermissionSelectionModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsViewPermission => Name.Equals("View", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Grouped representation of permissions for a Department-Role-Page combination
/// </summary>
public class RolePagePermissionGroupDto
{
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid PageId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public List<PermissionBadgeDto> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Permission badge information for display
/// </summary>
public class PermissionBadgeDto
{
    public Guid Id { get; set; }
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "primary";
}

/// <summary>
/// Request for batch create/update of permissions for a Department-Role-Page combination
/// </summary>
public class CreateOrUpdatePermissionBatchRequest
{
    public Guid? DepartmentId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PageId { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}
