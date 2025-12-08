using System.ComponentModel.DataAnnotations;

namespace AuthManagement.Models;

public class RoleFeatureMappingDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid FeatureId { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class CreateRoleFeatureMappingDto
{
    public Guid RoleId { get; set; }
    public Guid FeatureId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateRoleFeatureMappingDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid FeatureId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; }
}

public class RoleFeatureMappingFormModel
{
    public string Id { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Role is required")]
    public string RoleId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Feature is required")]
    public string FeatureId { get; set; } = string.Empty;
    
    public string DepartmentId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
