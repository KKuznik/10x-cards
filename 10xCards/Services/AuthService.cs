using _10xCards.Database.Entities;
using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace _10xCards.Services;

/// <summary>
/// Service for authentication operations including user registration and JWT token generation
/// </summary>
public sealed class AuthService : IAuthService {
	private readonly UserManager<User> _userManager;
	private readonly IConfiguration _configuration;
	private readonly ILogger<AuthService> _logger;

	public AuthService(
		UserManager<User> userManager,
		IConfiguration configuration,
		ILogger<AuthService> logger) {
		_userManager = userManager;
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<Result<AuthResponse>> RegisterUserAsync(
		RegisterRequest request,
		CancellationToken cancellationToken = default) {

		// Early return: Check if email already exists
		var existingUser = await _userManager.FindByEmailAsync(request.Email);
		if (existingUser is not null) {
			_logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
			return Result<AuthResponse>.Failure(new Dictionary<string, List<string>> {
				{ "email", new List<string> { "Email is already registered" } }
			});
		}

		// Create new user
		var user = new User {
			Id = Guid.NewGuid(),
			UserName = request.Email,
			Email = request.Email,
			EmailConfirmed = false
		};

		// Attempt to create user with password
		var result = await _userManager.CreateAsync(user, request.Password);

		// Early return: Handle creation failure
		if (!result.Succeeded) {
			_logger.LogWarning("User registration failed for email: {Email}. Errors: {Errors}",
				request.Email,
				string.Join(", ", result.Errors.Select(e => e.Description)));

			var errors = new Dictionary<string, List<string>>();
			foreach (var error in result.Errors) {
				var key = error.Code.Contains("Password") ? "password" : "email";
				if (!errors.ContainsKey(key)) {
					errors[key] = new List<string>();
				}
				errors[key].Add(error.Description);
			}

			return Result<AuthResponse>.Failure(errors);
		}

		// Generate JWT token
		var token = GenerateJwtToken(user);
		var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "1440");
		var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

		_logger.LogInformation("User registered successfully: {UserId}, Email: {Email}", user.Id, user.Email);

		// Happy path: Return success response
		var response = new AuthResponse {
			UserId = user.Id,
			Email = user.Email,
			Token = token,
			ExpiresAt = expiresAt
		};

		return Result<AuthResponse>.Success(response);
	}

	public async Task<Result<AuthResponse>> LoginUserAsync(
		LoginRequest request,
		CancellationToken cancellationToken = default) {

		// Early return: Find user by email
		var user = await _userManager.FindByEmailAsync(request.Email);
		if (user is null) {
			_logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
			return Result<AuthResponse>.Failure("Invalid email or password");
		}

		// Early return: Verify password
		var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
		if (!isPasswordValid) {
			_logger.LogWarning("Login attempt with invalid password for user: {UserId}", user.Id);
			return Result<AuthResponse>.Failure("Invalid email or password");
		}

		// Generate JWT token
		var token = GenerateJwtToken(user);
		var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "1440");
		var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

		_logger.LogInformation("User logged in successfully: {UserId}, Email: {Email}", user.Id, user.Email);

		// Happy path: Return success response
		var response = new AuthResponse {
			UserId = user.Id,
			Email = user.Email,
			Token = token,
			ExpiresAt = expiresAt
		};

		return Result<AuthResponse>.Success(response);
	}

	public async Task<Result<bool>> LogoutUserAsync(
		Guid userId,
		CancellationToken cancellationToken = default) {

		try {
			// Log successful logout for audit trail
			_logger.LogInformation(
				"User logged out successfully. UserId: {UserId}, Timestamp: {Timestamp}",
				userId,
				DateTime.UtcNow);

			// For stateless JWT, logout is primarily client-side
			// Token remains valid until expiration
			// Future enhancement: implement token blacklist

			return await Task.FromResult(Result<bool>.Success(true));
		} catch (Exception ex) {
			_logger.LogError(ex,
				"Logout operation failed. UserId: {UserId}",
				userId);

			return Result<bool>.Failure("An error occurred while processing logout");
		}
	}

	/// <summary>
	/// Generates a JWT token for the authenticated user
	/// </summary>
	private string GenerateJwtToken(User user) {
		var jwtSettings = _configuration.GetSection("JwtSettings");
		var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new List<Claim> {
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Email, user.Email ?? string.Empty),
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "1440");

		var token = new JwtSecurityToken(
			issuer: jwtSettings["Issuer"],
			audience: jwtSettings["Audience"],
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}

