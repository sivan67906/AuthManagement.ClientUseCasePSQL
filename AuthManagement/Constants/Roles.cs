namespace AuthManagement.Constants;

/// <summary>
/// Department-specific role constants for consistent role references throughout the application.
/// These role names must match exactly with the roles defined in the database seed data.
/// System roles (SuperAdmin, DepartmentAdmin, PendingUser) are in SystemRoles.cs
/// </summary>
public static class UIRoles
{
    // Finance Department Roles
    public const string FinanceAdmin = "FinanceAdmin";
    public const string FinanceManager = "FinanceManager";
    public const string FinanceSupervisor = "FinanceSupervisor";
    public const string FinanceStaff = "FinanceStaff";
    public const string FinanceIntern = "FinanceIntern";
    public const string FinanceAnalyst = "FinanceAnalyst";
    
    // Marketing Department Roles
    public const string MarketingManager = "MarketingManager";
    public const string MarketingSupervisor = "MarketingSupervisor";
    public const string MarketingStaff = "MarketingStaff";
    public const string MarketingIntern = "MarketingIntern";
    
    // HR Department Roles
    public const string HRAdmin = "HRAdmin";
    public const string HRManager = "HRManager";
    public const string HRExecutive = "HRExecutive";
    public const string HRStaff = "HRStaff";
    
    // Legacy/Generic Roles
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Staff = "Staff";
    public const string Accountant = "Accountant";
    public const string Auditor = "Auditor";
}

/// <summary>
/// Department name constants for consistent department references.
/// </summary>
public static class Departments
{
    public const string Finance = "Finance";
    public const string Marketing = "Marketing";
    public const string HR = "HR";
}

/// <summary>
/// Permission name constants for consistent permission checking.
/// </summary>
public static class PermissionNames
{
    public const string Create = "Create";
    public const string View = "View";
    public const string Update = "Update";
    public const string Delete = "Delete";
}

