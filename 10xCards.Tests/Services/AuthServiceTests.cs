using _10xCards.Database.Entities;
using _10xCards.Models.Requests;
using _10xCards.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace _10xCards.Tests.Services;

public class AuthServiceTests {
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthService _authService;

    public AuthServiceTests() {
        // Mock UserManager (requires mocking UserStore)
        var userStore = Substitute.For<IUserStore<User>>();
        _userManager = Substitute.For<UserManager<User>>(
            userStore, null, null, null, null, null, null, null, null);

        // Mock Configuration with JWT settings
        var configData = new Dictionary<string, string?> {
            { "JwtSettings:SecretKey", "super-secret-key-for-testing-purposes-that-is-long-enough-to-meet-requirements-123456" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpirationInMinutes", "60" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Mock Logger
        _logger = Substitute.For<ILogger<AuthService>>();

        _authService = new AuthService(_userManager, _configuration, _logger);
    }

    #region RegisterUserAsync Tests

    [Fact]
    public async Task RegisterUserAsync_ValidRequest_ReturnsSuccessWithToken() {
        // Arrange
        var request = new RegisterRequest {
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((User?)null);
        _userManager.CreateAsync(Arg.Any<User>(), request.Password)
            .Returns(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.UserId.Should().NotBeEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.Received(1).CreateAsync(Arg.Any<User>(), request.Password);
    }

    [Fact]
    public async Task RegisterUserAsync_DuplicateEmail_ReturnsFailure() {
        // Arrange
        var existingUser = new User {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            UserName = "existing@example.com"
        };

        var request = new RegisterRequest {
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(existingUser);

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainKey("email");
        result.Errors!["email"].Should().Contain("Email is already registered");

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterUserAsync_UserCreationFails_ReturnsFailureWithErrors() {
        // Arrange
        var request = new RegisterRequest {
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        var identityErrors = new[] {
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" },
            new IdentityError { Code = "PasswordRequiresUpper", Description = "Password requires uppercase" }
        };

        _userManager.FindByEmailAsync(request.Email).Returns((User?)null);
        _userManager.CreateAsync(Arg.Any<User>(), request.Password)
            .Returns(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainKey("password");
        result.Errors!["password"].Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task RegisterUserAsync_DuplicateEmail_LogsWarning() {
        // Arrange
        var existingUser = new User {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            UserName = "existing@example.com"
        };

        var request = new RegisterRequest {
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(existingUser);

        // Act
        await _authService.RegisterUserAsync(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("existing email")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region LoginUserAsync Tests

    [Fact]
    public async Task LoginUserAsync_ValidCredentials_ReturnsSuccessWithToken() {
        // Arrange
        var user = new User {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com"
        };

        var request = new LoginRequest {
            Email = "user@example.com",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(true);

        // Act
        var result = await _authService.LoginUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.Received(1).CheckPasswordAsync(user, request.Password);
    }

    [Fact]
    public async Task LoginUserAsync_NonExistentEmail_ReturnsFailure() {
        // Arrange
        var request = new LoginRequest {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((User?)null);

        // Act
        var result = await _authService.LoginUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");

        await _userManager.Received(1).FindByEmailAsync(request.Email);
        await _userManager.DidNotReceive().CheckPasswordAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginUserAsync_InvalidPassword_ReturnsFailure() {
        // Arrange
        var user = new User {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com"
        };

        var request = new LoginRequest {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(false);

        // Act
        var result = await _authService.LoginUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");

        await _userManager.Received(1).CheckPasswordAsync(user, request.Password);
    }

    [Fact]
    public async Task LoginUserAsync_NonExistentEmail_LogsWarning() {
        // Arrange
        var request = new LoginRequest {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((User?)null);

        // Act
        await _authService.LoginUserAsync(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("non-existent email")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LoginUserAsync_InvalidPassword_LogsWarning() {
        // Arrange
        var user = new User {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com"
        };

        var request = new LoginRequest {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(false);

        // Act
        await _authService.LoginUserAsync(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("invalid password")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region LogoutUserAsync Tests

    [Fact]
    public async Task LogoutUserAsync_ValidUserId_ReturnsSuccess() {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _authService.LogoutUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutUserAsync_EmptyGuid_ReturnsSuccess() {
        // Arrange
        var userId = Guid.Empty;

        // Act
        var result = await _authService.LogoutUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutUserAsync_ValidUserId_LogsInformation() {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _authService.LogoutUserAsync(userId);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("logged out successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region JWT Token Generation Tests

    [Fact]
    public async Task RegisterUserAsync_GeneratesValidJwtToken() {
        // Arrange
        var request = new RegisterRequest {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((User?)null);
        _userManager.CreateAsync(Arg.Any<User>(), request.Password)
            .Returns(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().NotBeNullOrEmpty();

        // JWT token should have 3 parts separated by dots
        var tokenParts = result.Value.Token.Split('.');
        tokenParts.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoginUserAsync_GeneratesValidJwtToken() {
        // Arrange
        var user = new User {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com"
        };

        var request = new LoginRequest {
            Email = "user@example.com",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(true);

        // Act
        var result = await _authService.LoginUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().NotBeNullOrEmpty();

        // JWT token should have 3 parts separated by dots
        var tokenParts = result.Value.Token.Split('.');
        tokenParts.Should().HaveCount(3);
    }

    #endregion
}

