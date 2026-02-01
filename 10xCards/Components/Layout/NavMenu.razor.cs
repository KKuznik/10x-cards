using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace _10xCards.Components.Layout;

/// <summary>
/// Navigation menu component with authentication-aware links
/// </summary>
public partial class NavMenu : ComponentBase, IDisposable {
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private ILogger<NavMenu> Logger { get; set; } = default!;

    private bool isAuthenticated = false;
    private bool navMenuExpanded = false;

    /// <summary>
    /// CSS class for navigation menu state
    /// </summary>
    private string NavMenuCssClass => navMenuExpanded ? "expanded" : "collapsed";

    /// <summary>
    /// Toggle navigation menu visibility
    /// </summary>
    private void ToggleNavMenu() {
        navMenuExpanded = !navMenuExpanded;
    }

    /// <summary>
    /// Collapse navigation menu
    /// </summary>
    private void CollapseNavMenu() {
        navMenuExpanded = false;
    }

    /// <summary>
    /// Handle keyboard events for accessibility
    /// </summary>
    private void HandleKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e) {
        // Close menu when Escape key is pressed (WCAG 2.0 compliance)
        if (e.Key == "Escape" && navMenuExpanded) {
            CollapseNavMenu();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Initialize component and check authentication state
    /// </summary>
    protected override async Task OnInitializedAsync() {
        // Subscribe to authentication state changes
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;

        await UpdateAuthenticationState();
    }

    /// <summary>
    /// Handle authentication state changes
    /// </summary>
    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task) {
        await UpdateAuthenticationState();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Update authentication state
    /// </summary>
    private async Task UpdateAuthenticationState() {
        try {
            // Get authentication state from provider
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // Check if user is authenticated
            isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        } catch (InvalidOperationException ex) {
            // Authentication provider not properly configured
            Logger.LogWarning(ex, "Authentication provider not properly configured in NavMenu");
            isAuthenticated = false;
        } catch (Exception ex) {
            // If authentication check fails, assume not authenticated
            Logger.LogError(ex, "Unexpected error checking authentication state in NavMenu");
            isAuthenticated = false;
        }
    }

    /// <summary>
    /// Dispose and unsubscribe from events
    /// </summary>
    public void Dispose() {
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}

