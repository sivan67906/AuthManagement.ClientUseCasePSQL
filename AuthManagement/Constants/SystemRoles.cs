namespace AuthManagement.Constants;

/// <summary>
/// System-level roles that are hardcoded and used for core authorization
/// All other roles are dynamic and managed through the database
/// These must match the SystemRoles in the backend API
/// </summary>
public static class SystemRoles
{
    // SuperAdmin - Has unrestricted access to everything across all departments
    public const string SuperAdmin = "SuperAdmin";
    
    // DepartmentAdmin - Has full access within their assigned department only
    public const string DepartmentAdmin = "DepartmentAdmin";
    
    // PendingUser - Newly registered users waiting for role assignment
    public const string PendingUser = "PendingUser";
}
