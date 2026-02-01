using _10xCards.Utilities;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace _10xCards.Tests.Utilities;

public class JwtHelperTests {

    /// <summary>
    /// Helper method to generate a valid JWT token for testing
    /// </summary>
    private string GenerateTestToken(string userId, string? claimType = null) {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-that-is-long-enough-for-testing-purposes-123456"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(claimType ?? ClaimTypes.NameIdentifier, userId)
        };

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void ExtractUserIdFromToken_ValidToken_ReturnsUserId() {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var token = GenerateTestToken(expectedUserId.ToString());

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void ExtractUserIdFromToken_NullToken_ReturnsGuidEmpty() {
        // Arrange
        string? token = null;

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_EmptyString_ReturnsGuidEmpty() {
        // Arrange
        var token = string.Empty;

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_WhitespaceString_ReturnsGuidEmpty() {
        // Arrange
        var token = "   ";

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_InvalidTokenFormat_ReturnsGuidEmpty() {
        // Arrange
        var token = "this-is-not-a-valid-jwt-token";

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_TokenWithoutNameIdentifierClaim_ReturnsGuidEmpty() {
        // Arrange
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-that-is-long-enough-for-testing-purposes-123456"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim("email", "test@example.com"),
            new Claim("role", "user")
        };

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(tokenString);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_TokenWithInvalidGuid_ReturnsGuidEmpty() {
        // Arrange
        var token = GenerateTestToken("not-a-valid-guid");

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_TokenWithSubClaim_ReturnsUserId() {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var token = GenerateTestToken(expectedUserId.ToString(), "sub");

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void ExtractUserIdFromToken_TokenWithEmptyClaimValue_ReturnsGuidEmpty() {
        // Arrange
        var token = GenerateTestToken(string.Empty);

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ExtractUserIdFromToken_MalformedToken_ReturnsGuidEmpty() {
        // Arrange
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.payload";

        // Act
        var result = JwtHelper.ExtractUserIdFromToken(token);

        // Assert
        result.Should().Be(Guid.Empty);
    }
}

