using _10xCards.Models.Requests;
using _10xCards.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Components.Pages;

/// <summary>
/// Code-behind for the user login page
/// </summary>
/// <remarks>
/// SECURITY RECOMMENDATION: Implement rate limiting for login attempts
/// to prevent brute-force attacks. This should be implemented at the 
/// middleware/server level using ASP.NET Core Rate Limiting.
/// 
/// Example implementation in Program.cs:
/// <code>
/// using System.Threading.RateLimiting;
/// using Microsoft.AspNetCore.RateLimiting;
/// 
/// builder.Services.AddRateLimiter(options => {
///     options.AddFixedWindowLimiter("login", options => {
///         options.PermitLimit = 5;
///         options.Window = TimeSpan.FromMinutes(1);
///         options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
///         options.QueueLimit = 0;
///     });
/// });
/// 
/// // In endpoint configuration:
/// app.MapPost("/api/auth/login", ...).RequireRateLimiting("login");
/// </code>
/// 
/// Additional security measures:
/// - Implement exponential backoff for repeated failures (e.g., 1min, 5min, 15min)
/// - Add CAPTCHA after 3 failed attempts from same IP
/// - Monitor and alert on suspicious patterns (e.g., distributed attacks)
/// - Consider account lockout after 10 failed attempts in 24h
/// - Log failed login attempts with IP (not email) for security monitoring
/// </remarks>
public partial class Login : ComponentBase {
    // Error message constants for consistency
    private const string ERROR_INVALID_EMAIL = "Email jest nieprawidłowy";
    private const string ERROR_INVALID_PASSWORD = "Hasło jest nieprawidłowe";
    private const string ERROR_INVALID_CREDENTIALS = "Nieprawidłowy email lub hasło";
    private const string ERROR_GENERIC = "Wystąpił błąd. Spróbuj ponownie później.";

    [Inject]
    private IAuthService AuthService { get; set; } = default!;

    [Inject]
    private IClientAuthenticationService ClientAuthService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<Login> Logger { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private LoginViewModel loginModel = new();
    private string? errorMessage;
    private bool isSubmitting;
    private EditContext? editContext;

    /// <summary>
    /// Initialize component and setup edit context
    /// </summary>
    protected override void OnInitialized() {
        editContext = new EditContext(loginModel);
        base.OnInitialized();
    }

    /// <summary>
    /// Check if a field has validation errors
    /// </summary>
    private bool HasFieldError(string fieldName) {
        if (editContext == null) return false;
        var fieldIdentifier = editContext.Field(fieldName);
        return editContext.GetValidationMessages(fieldIdentifier).Any();
    }

    /// <summary>
    /// Set focus to first input field for better accessibility
    /// </summary>
    private async Task SetFocusToFirstFieldAsync() {
        try {
            await JSRuntime.InvokeVoidAsync("setFocusById", "email");
        } catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to set focus to first field");
        }
    }

    /// <summary>
    /// Handles form submission after successful validation
    /// </summary>
    private async Task HandleValidSubmit() {
        // Guard clause: prevent multiple submissions
        if (isSubmitting) {
            return;
        }

        try {
            isSubmitting = true;
            errorMessage = null;

            // Guard clause: Additional server-side validation for defense in depth
            if (string.IsNullOrWhiteSpace(loginModel.Email) || loginModel.Email.Length > 255) {
                errorMessage = ERROR_INVALID_EMAIL;
                return;
            }

            if (string.IsNullOrWhiteSpace(loginModel.Password) || loginModel.Password.Length > 100) {
                errorMessage = ERROR_INVALID_PASSWORD;
                return;
            }

            // Map ViewModel to Request DTO
            var request = new LoginRequest {
                Email = loginModel.Email,
                Password = loginModel.Password
            };

            // Call authentication service with 30 second timeout
            var result = await AuthService.LoginUserAsync(request, CreateTimeoutToken(30));

            // Handle error cases first (early return pattern)
            if (!result.IsSuccess) {
                errorMessage = result.ErrorMessage ?? ERROR_INVALID_CREDENTIALS;
                // WCAG: Focus first field for better accessibility
                await SetFocusToFirstFieldAsync();
                return;
            }

            // Happy path: successful login
            if (result.Value != null) {
                // Save authentication state via client authentication service
                await ClientAuthService.LoginAsync(result.Value.Token, result.Value.ExpiresAt, result.Value.Email);

                // Navigate to home page
                NavigationManager.NavigateTo("/", forceLoad: true);
            }
        } catch (TaskCanceledException ex) {
            // Handle timeout
            Logger.LogWarning(ex, "Login request timeout after 30 seconds");
            errorMessage = "Żądanie przekroczyło limit czasu. Spróbuj ponownie.";
        } catch (HttpRequestException ex) {
            // Handle network errors
            Logger.LogWarning(ex, "Network error during login attempt");
            errorMessage = "Błąd połączenia z serwerem. Sprawdź połączenie internetowe.";
        } catch (Exception ex) {
            // Log the exception without exposing user email for security
            // SECURITY: Do not log sensitive user information in production
            Logger.LogError(ex, "Unexpected error during login attempt");

            // Handle unexpected errors - generic message to prevent information disclosure
            errorMessage = ERROR_GENERIC;
        } finally {
            isSubmitting = false;
        }
    }

    /// <summary>
    /// Creates a CancellationToken with a timeout to prevent hanging operations
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <returns>CancellationToken that will be cancelled after timeout</returns>
    private static CancellationToken CreateTimeoutToken(int timeoutSeconds) {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        return cts.Token;
    }

    /// <summary>
    /// ViewModel for login form with validation attributes
    /// </summary>
    public class LoginViewModel {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        public string Password { get; set; } = string.Empty;
    }
}

