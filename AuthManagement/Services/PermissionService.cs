using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using AuthManagement.Models;
using AuthManagement.Constants;

namespace AuthManagement.Services;

public class PermissionService
{
    private readonly RBACService _rbacService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private UserAccessDto? _userAccess;
    private DateTime? _lastLoaded;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public PermissionService(RBACService rbacService, AuthenticationStateProvider authStateProvider)
    {
        _rbacService = rbacService;
        _authStateProvider = authStateProvider;
    }

    private async Task<UserAccessDto?> GetUserAccessAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _userAccess != null && _lastLoaded.HasValue &&
            DateTime.UtcNow - _lastLoaded.Value < _cacheExpiration)
        {
            return _userAccess;
        }

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var email = user.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var response = await _rbacService.GetUserAccessByEmailAsync(email);
        if (response.Success && response.Data != null)
        {
            _userAccess = response.Data;
            _lastLoaded = DateTime.UtcNow;
            return _userAccess;
        }

        return null;
    }

    public async Task<bool> HasPermissionAsync(string pageName, string permissionName)
    {
        var userAccess = await GetUserAccessAsync();
        if (userAccess == null) return false;

        // SuperAdmin has all permissions
        if (userAccess.Roles.Contains(SystemRoles.SuperAdmin))
        {
            return true;
        }

        //  SPECIAL CASE: Admin can only VIEW Department page
        if (IsDepartmentPage(pageName) && IsAdminRole(userAccess.Roles) && !IsSuperAdmin(userAccess.Roles))
        {
            return permissionName.Equals("View", StringComparison.OrdinalIgnoreCase);
        }

        // Check page-specific permissions first
        if (userAccess.PagePermissions != null && userAccess.PagePermissions.Count > 0)
        {
            return userAccess.HasPermissionOnPage(pageName, permissionName);
        }

        // Fallback: Check global permissions (for backward compatibility)
        return userAccess.Permissions.Contains(permissionName);
    }

    public async Task<PagePermissions> GetPagePermissionsAsync(string pageName)
    {
        var userAccess = await GetUserAccessAsync();
        if (userAccess == null)
        {
            return new PagePermissions();
        }

        // SuperAdmin has all permissions
        if (userAccess.Roles.Contains(SystemRoles.SuperAdmin))
        {
            return new PagePermissions
            {
                CanView = true,
                CanAdd = true,
                CanEdit = true,
                CanDelete = true
            };
        }

        var pagePermissions = new PagePermissions();

        // ============================================================
        //  SPECIAL CASE: Admin can ONLY VIEW Department page
        // ============================================================
        if (IsDepartmentPage(pageName) && IsAdminRole(userAccess.Roles) && !IsSuperAdmin(userAccess.Roles))
        {
            pagePermissions.CanView = true;
            pagePermissions.CanAdd = false;
            pagePermissions.CanEdit = false;
            pagePermissions.CanDelete = false;
            return pagePermissions;
        }

        // ============================================================
        // Use page-specific permissions from PagePermissions dictionary
        // ============================================================
        if (userAccess.PagePermissions != null && userAccess.PagePermissions.Count > 0)
        {
            // Get permissions for THIS PAGE ONLY
            var permsForThisPage = userAccess.GetPermissionsForPage(pageName);

            if (permsForThisPage.Any())
            {
                // Map permissions to boolean flags
                pagePermissions.CanView = permsForThisPage.Any(p =>
                    p.Equals("View", StringComparison.OrdinalIgnoreCase));

                pagePermissions.CanAdd = permsForThisPage.Any(p =>
                    p.Equals("Create", StringComparison.OrdinalIgnoreCase));

                pagePermissions.CanEdit = permsForThisPage.Any(p =>
                    p.Equals("Update", StringComparison.OrdinalIgnoreCase));

                pagePermissions.CanDelete = permsForThisPage.Any(p =>
                    p.Equals("Delete", StringComparison.OrdinalIgnoreCase));

                return pagePermissions;
            }
        }

        // ============================================================
        // Fallback: Use global permissions (for backward compatibility)
        // ============================================================
        if (userAccess.Permissions != null && userAccess.Permissions.Any())
        {
            pagePermissions.CanView = userAccess.Permissions.Any(p =>
                p.Equals("View", StringComparison.OrdinalIgnoreCase));

            pagePermissions.CanAdd = userAccess.Permissions.Any(p =>
                p.Equals("Create", StringComparison.OrdinalIgnoreCase));

            pagePermissions.CanEdit = userAccess.Permissions.Any(p =>
                p.Equals("Update", StringComparison.OrdinalIgnoreCase));

            pagePermissions.CanDelete = userAccess.Permissions.Any(p =>
                p.Equals("Delete", StringComparison.OrdinalIgnoreCase));
        }

        // If still no permissions found, apply role-based fallback
        if (!pagePermissions.HasAnyPermission)
        {
            ApplyRoleBasedPermissions(userAccess.Roles, pagePermissions, pageName);
        }

        return pagePermissions;
    }

    private void ApplyRoleBasedPermissions(List<string> roles, PagePermissions permissions, string pageName)
    {
        //  SPECIAL CASE: Admin roles can only VIEW Department page
        if (IsDepartmentPage(pageName) && IsAdminRole(roles) && !IsSuperAdmin(roles))
        {
            permissions.CanView = true;
            permissions.CanAdd = false;
            permissions.CanEdit = false;
            permissions.CanDelete = false;
            return;
        }

        // If user has Admin roles (FinanceAdmin, HRAdmin, etc.) - Full CRUD on all OTHER pages
        if (roles.Any(r => r.Contains("Admin") && r != SystemRoles.SuperAdmin))
        {
            permissions.CanView = true;
            permissions.CanAdd = true;
            permissions.CanEdit = true;
            permissions.CanDelete = true;
        }
        // Manager roles
        else if (roles.Any(r => r.Contains("Manager")))
        {
            permissions.CanView = true;
            // Managers have full CRUD only on Test pages
            if (pageName.StartsWith("Test", StringComparison.OrdinalIgnoreCase))
            {
                permissions.CanAdd = true;
                permissions.CanEdit = true;
                permissions.CanDelete = true;
            }
        }
        // Supervisor role support
        else if (roles.Any(r => r.Contains("Supervisor")))
        {
            permissions.CanView = true;
            permissions.CanAdd = true;
            permissions.CanEdit = true;
            permissions.CanDelete = false;  // Supervisors cannot delete
        }
        // Analyst/Executive roles
        else if (roles.Any(r => r.Contains("Analyst") || r.Contains("Executive")))
        {
            permissions.CanView = true;
            permissions.CanAdd = true;
            permissions.CanEdit = false;
            permissions.CanDelete = false;
        }
        // Staff roles - Create and View only on Test pages
        else if (roles.Any(r => r.Contains("Staff")))
        {
            permissions.CanView = true;
            // Staff can create only on Test pages
            if (pageName.StartsWith("Test", StringComparison.OrdinalIgnoreCase))
            {
                permissions.CanAdd = true;
            }
            permissions.CanEdit = false;
            permissions.CanDelete = false;
        }
        // Intern roles - view only
        else if (roles.Any(r => r.Contains("Intern")))
        {
            permissions.CanView = true;
            permissions.CanAdd = false;
            permissions.CanEdit = false;
            permissions.CanDelete = false;
        }
    }

    //  Helper methods for Department page detection
    private bool IsDepartmentPage(string pageName)
    {
        if (string.IsNullOrEmpty(pageName))
            return false;

        return pageName.Equals("Department", StringComparison.OrdinalIgnoreCase) ||
               pageName.Equals("/department", StringComparison.OrdinalIgnoreCase) ||
               pageName.Contains("department", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAdminRole(List<string> roles)
    {
        return roles.Any(r => r.Contains("Admin", StringComparison.OrdinalIgnoreCase) &&
                             !r.Equals(SystemRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsSuperAdmin(List<string> roles)
    {
        return roles.Any(r => r.Equals(SystemRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> IsSuperAdminAsync()
    {
        var userAccess = await GetUserAccessAsync();
        return userAccess?.Roles.Contains(SystemRoles.SuperAdmin) ?? false;
    }

    public async Task<List<string>> GetUserRolesAsync()
    {
        var userAccess = await GetUserAccessAsync();
        return userAccess?.Roles ?? new List<string>();
    }

    public async Task<(Guid? DepartmentId, string? DepartmentName)> GetUserDepartmentAsync()
    {
        var userAccess = await GetUserAccessAsync();
        if (userAccess == null)
        {
            return (null, null);
        }
        return (userAccess.DepartmentId, userAccess.DepartmentName);
    }

    public void ClearCache()
    {
        _userAccess = null;
        _lastLoaded = null;
    }
}

public class PagePermissions
{
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public bool HasAnyPermission => CanView || CanAdd || CanEdit || CanDelete;
}

// ============================================================
// PERMISSION RULES SUMMARY:
//
// SuperAdmin:
//   - ALL pages: Full CRUD (including all departments)
//
// Admin (FinanceAdmin, HRAdmin, etc.):
//   - Department page: View ONLY (can only see their own department)
//   - All other pages: Full CRUD (within their department)
//
// Manager:
//   - Test pages: Full CRUD
//   - Other pages: View only
//
// Supervisor:
//   - All pages: Create, Update, View (NO Delete)
//
// Staff:
//   - Test pages: Create, View (NO Update, Delete)
//   - Other pages: View only
//
// Intern:
//   - All pages: View only
// ============================================================
