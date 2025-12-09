using Microsoft.AspNetCore.Components;
using AuthManagement.Services;

namespace AuthManagement.Shared;

/// <summary>
/// Base component for pages that require page-level access control
/// Inherit from this component to automatically enforce page access based on role mappings
/// </summary>
public abstract class PageAccessBase : ComponentBase
{
    [Inject] protected PageAccessService PageAccessService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected bool IsCheckingAccess { get; set; } = true;
    protected bool HasPageAccess { get; set; } = false;
    protected string? AccessDeniedReason { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await CheckPageAccessAsync();
        await base.OnInitializedAsync();
    }

    private async Task CheckPageAccessAsync()
    {
        IsCheckingAccess = true;

        var result = await PageAccessService.CheckCurrentPageAccessAsync();

        if (!result.HasAccess)
        {
            // Store the reason for debugging
            AccessDeniedReason = result.Reason;

            // Redirect based on authentication status
            if (result.RequiresAuthentication)
            {
                NavigationManager.NavigateTo("/login", true);
            }
            else
            {
                NavigationManager.NavigateTo("/access-denied", true);
            }
            return;
        }

        HasPageAccess = true;
        IsCheckingAccess = false;
    }
}
