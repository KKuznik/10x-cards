using _10xCards.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace _10xCards.Components.Layout;

/// <summary>
/// User menu component for authentication actions (login, register, logout)
/// </summary>
public partial class UserMenu : ComponentBase, IDisposable {
    [Inject]
    private IClientAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private ILogger<UserMenu> Logger { get; set; } = default!;

    private bool isAuthenticated = false;
    private string? username;

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
            // Check if user is authenticated
            isAuthenticated = await AuthService.IsAuthenticatedAsync();

            // Get username if authenticated
            if (isAuthenticated) {
                username = await AuthService.GetUsernameAsync();
            } else {
                username = null;
            }
        } catch (InvalidOperationException ex) {
            // Authentication service not properly configured
            Logger.LogWarning(ex, "Authentication service not properly configured in UserMenu");
            isAuthenticated = false;
            username = null;
        } catch (Exception ex) {
            // If authentication check fails, assume not authenticated
            Logger.LogError(ex, "Unexpected error checking authentication state in UserMenu");
            isAuthenticated = false;
            username = null;
        }
    }

    /// <summary>
    /// Handle logout action
    /// </summary>
    private async Task HandleLogout() {
        try {
            // Logout via authentication service
            await AuthService.LogoutAsync();

            // Navigate to login page with force reload
            NavigationManager.NavigateTo("/login", forceLoad: true);
        } catch (Exception ex) {
            // Log the error and navigate to login
            Logger.LogError(ex, "Error during logout in UserMenu");
            NavigationManager.NavigateTo("/login", forceLoad: true);
        }
    }

    /// <summary>
    /// Navigate to login page
    /// </summary>
    private void NavigateToLogin() {
        NavigationManager.NavigateTo("/login");
    }

    /// <summary>
    /// Navigate to register page
    /// </summary>
    private void NavigateToRegister() {
        NavigationManager.NavigateTo("/register");
    }

    /// <summary>
    /// Dispose and unsubscribe from events
    /// </summary>
    public void Dispose() {
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}

