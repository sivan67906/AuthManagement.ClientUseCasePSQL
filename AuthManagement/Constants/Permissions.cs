namespace AuthManagement.Constants;

/// <summary>
/// Permission constants that match the database Permission records.
/// Primary CRUD permissions are: Create, View, Update, Delete
/// </summary>
public static class Permissions
{
    // Primary CRUD Permission Types (these are what's stored in DB and used for RBAC)
    public const string Create = "Create";
    public const string View = "View";
    public const string Update = "Update";
    public const string Delete = "Delete";
    
    // Department Permissions (for future granular access)
    public const string CreateDepartment = "Department.Create";
    public const string ViewDepartment = "Department.View";
    public const string UpdateDepartment = "Department.Update";
    public const string DeleteDepartment = "Department.Delete";

    // Role Permissions
    public const string CreateRole = "Role.Create";
    public const string ViewRole = "Role.View";
    public const string UpdateRole = "Role.Update";
    public const string DeleteRole = "Role.Delete";

    // Permission Management Permissions
    public const string CreatePermission = "Permission.Create";
    public const string ViewPermission = "Permission.View";
    public const string UpdatePermission = "Permission.Update";
    public const string DeletePermission = "Permission.Delete";

    // Feature Permissions
    public const string CreateFeature = "Feature.Create";
    public const string ViewFeature = "Feature.View";
    public const string UpdateFeature = "Feature.Update";
    public const string DeleteFeature = "Feature.Delete";

    // Page Permissions
    public const string CreatePage = "Page.Create";
    public const string ViewPage = "Page.View";
    public const string UpdatePage = "Page.Update";
    public const string DeletePage = "Page.Delete";

    // Mapping Permissions
    public const string ManageRoleHierarchy = "RoleHierarchy.Manage";
    public const string ManageUserRoleMapping = "UserRoleMapping.Manage";
    public const string ManageRolePermissionMapping = "RolePermissionMapping.Manage";
    public const string ManagePagePermissionMapping = "PagePermissionMapping.Manage";
    public const string ManagePageFeatureMapping = "PageFeatureMapping.Manage";
    public const string ManageRoleFeatureMapping = "RoleFeatureMapping.Manage";
    public const string ManageRoleDepartmentMapping = "RoleDepartmentMapping.Manage";
    public const string ManageRolePagePermissionMapping = "RolePagePermissionMapping.Manage";

    // Account Settings Permissions
    public const string ChangePassword = "ChangePassword";
    public const string ManageTwoFactor = "TwoFactor.Manage";
    public const string ManageAuthenticator = "Authenticator.Manage";
    public const string ManageAddresses = "Addresses.Manage";

    // User Management Permissions
    public const string ManageUsers = "User.Manage";
    public const string ViewUsers = "User.View";

    // Profile Permissions
    public const string ViewProfile = "Profile.View";
    public const string EditProfile = "Profile.Edit";
}
