using _10xCards.Models.Requests;
using _10xCards.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;

namespace _10xCards.Components.Pages;

/// <summary>
/// Code-behind for the user registration page
/// </summary>
/// <remarks>
/// SECURITY RECOMMENDATION: Implement rate limiting for registration attempts
/// to prevent automated account creation and abuse. This should be implemented 
/// at the middleware/server level using ASP.NET Core Rate Limiting.
/// 
/// Example implementation in Program.cs:
/// <code>
/// using System.Threading.RateLimiting;
/// using Microsoft.AspNetCore.RateLimiting;
/// 
/// builder.Services.AddRateLimiter(options => {
///     options.AddFixedWindowLimiter("register", options => {
///         options.PermitLimit = 3;
///         options.Window = TimeSpan.FromHours(1);
///         options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
///         options.QueueLimit = 0;
///     });
/// });
/// 
/// // In endpoint configuration:
/// app.MapPost("/api/auth/register", ...).RequireRateLimiting("register");
/// </code>
/// 
/// Additional security measures:
/// - CRITICAL: Implement email verification before account activation
/// - Add CAPTCHA (e.g., reCAPTCHA v3) to registration form
/// - Monitor for bot patterns: rapid submissions, similar email patterns
/// - Implement honeypot fields (hidden fields that humans won't fill)
/// - Block disposable email domains if necessary
/// - Consider phone verification for additional security
/// - Log registration attempts with IP for abuse monitoring
/// </remarks>
public partial class Register : ComponentBase {
    // Error message constants for consistency
    private const string ERROR_INVALID_EMAIL = "Email jest nieprawidłowy lub za długi";
    private const string ERROR_INVALID_PASSWORD_LENGTH = "Hasło musi mieć od 8 do 100 znaków";
    private const string ERROR_PASSWORD_MISMATCH = "Hasła muszą być identyczne";
    private const string ERROR_REGISTRATION_FAILED = "Wystąpił błąd podczas rejestracji. Spróbuj ponownie.";
    private const string ERROR_GENERIC = "Wystąpił błąd. Spróbuj ponownie później.";

    [Inject]
    private IAuthService AuthService { get; set; } = default!;

    [Inject]
    private IClientAuthenticationService ClientAuthService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<Register> Logger { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private RegisterViewModel registerModel = new();
    private string? errorMessage;
    private bool isSubmitting;
    private EditContext? editContext;

    /// <summary>
    /// Initialize component and setup edit context
    /// </summary>
    protected override void OnInitialized() {
        editContext = new EditContext(registerModel);
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
            if (string.IsNullOrWhiteSpace(registerModel.Email) || registerModel.Email.Length > 255) {
                errorMessage = ERROR_INVALID_EMAIL;
                StateHasChanged(); // Force immediate re-render in InteractiveServer mode
                return;
            }

            if (string.IsNullOrWhiteSpace(registerModel.Password) ||
                registerModel.Password.Length < 8 ||
                registerModel.Password.Length > 100) {
                errorMessage = ERROR_INVALID_PASSWORD_LENGTH;
                StateHasChanged(); // Force immediate re-render in InteractiveServer mode
                return;
            }

            if (registerModel.Password != registerModel.ConfirmPassword) {
                errorMessage = ERROR_PASSWORD_MISMATCH;
                StateHasChanged(); // Force immediate re-render in InteractiveServer mode
                return;
            }

            // Map ViewModel to Request DTO
            var request = new RegisterRequest {
                Email = registerModel.Email,
                Password = registerModel.Password,
                ConfirmPassword = registerModel.ConfirmPassword
            };

            // Call authentication service with 30 second timeout
            var result = await AuthService.RegisterUserAsync(request, CreateTimeoutToken(30));

            // Handle error cases first (early return pattern)
            if (!result.IsSuccess) {
                errorMessage = ExtractErrorMessage(result.Errors);
                StateHasChanged(); // Force immediate re-render in InteractiveServer mode
                                   // WCAG: Focus first field for better accessibility
                await SetFocusToFirstFieldAsync();
                return;
            }

            // Happy path: successful registration
            if (result.Value != null) {
                // Save authentication state via client authentication service
                await ClientAuthService.LoginAsync(result.Value.Token, result.Value.ExpiresAt, result.Value.Email);

                // Navigate to home page
                NavigationManager.NavigateTo("/", forceLoad: true);
            }
        } catch (TaskCanceledException ex) {
            // Handle timeout
            Logger.LogWarning(ex, "Registration request timeout after 30 seconds");
            errorMessage = "Żądanie przekroczyło limit czasu. Spróbuj ponownie.";
            StateHasChanged(); // Force immediate re-render in InteractiveServer mode
        } catch (HttpRequestException ex) {
            // Handle network errors
            Logger.LogWarning(ex, "Network error during registration attempt");
            errorMessage = "Błąd połączenia z serwerem. Sprawdź połączenie internetowe.";
            StateHasChanged(); // Force immediate re-render in InteractiveServer mode
        } catch (Exception ex) {
            // Log the exception without exposing user email for security/GDPR compliance
            // SECURITY: Do not log sensitive user information in production
            Logger.LogError(ex, "Unexpected error during registration attempt");

            // Handle unexpected errors - generic message to prevent information disclosure
            errorMessage = ERROR_GENERIC;
            StateHasChanged(); // Force immediate re-render in InteractiveServer mode
        } finally {
            isSubmitting = false;
        }
    }

    /// <summary>
    /// Extracts user-friendly error message from validation errors dictionary
    /// </summary>
    private string ExtractErrorMessage(Dictionary<string, List<string>>? errors) {
        // Guard clause: null or empty dictionary
        if (errors == null || errors.Count == 0) {
            return ERROR_REGISTRATION_FAILED;
        }

        // Try to get first error message from the dictionary
        var firstError = errors.FirstOrDefault();

        // Guard clause: check if value exists
        if (firstError.Value == null || firstError.Value.Count == 0) {
            return ERROR_REGISTRATION_FAILED;
        }

        // Return first error message or default
        return firstError.Value.FirstOrDefault() ?? ERROR_REGISTRATION_FAILED;
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
    /// ViewModel for registration form with validation attributes
    /// </summary>
    public class RegisterViewModel {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        [MaxLength(255, ErrorMessage = "Email nie może przekraczać 255 znaków")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
        [MaxLength(100, ErrorMessage = "Hasło nie może przekraczać 100 znaków")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
            ErrorMessage = "Hasło musi zawierać wielką literę, małą literę, cyfrę i znak specjalny")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [Compare(nameof(Password), ErrorMessage = "Hasła muszą być identyczne")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

