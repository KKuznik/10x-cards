namespace _10xCards.E2ETests.Helpers;

/// <summary>
/// Generates unique test data for E2E tests
/// Ensures test isolation by creating unique values for each test run
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Generate a unique email address for testing
    /// Format: test_{guid}@example.com
    /// </summary>
    public static string GenerateUniqueEmail()
    {
        return $"test_{Guid.NewGuid():N}@example.com";
    }

    /// <summary>
    /// Generate a valid password that meets all requirements:
    /// - At least 8 characters
    /// - Contains uppercase letter
    /// - Contains lowercase letter
    /// - Contains digit
    /// - Contains special character
    /// </summary>
    public static string GenerateValidPassword()
    {
        return "TestPass123!";
    }

    /// <summary>
    /// Generate a password that does NOT meet requirements (for negative testing)
    /// </summary>
    public static string GenerateInvalidPassword()
    {
        return "weak";
    }

    /// <summary>
    /// Generate a complete test user with unique credentials
    /// </summary>
    public static TestUser GenerateTestUser()
    {
        return new TestUser
        {
            Email = GenerateUniqueEmail(),
            Password = GenerateValidPassword()
        };
    }

    /// <summary>
    /// Generate a test user with an invalid password
    /// </summary>
    public static TestUser GenerateTestUserWithInvalidPassword()
    {
        return new TestUser
        {
            Email = GenerateUniqueEmail(),
            Password = GenerateInvalidPassword()
        };
    }
}

/// <summary>
/// Represents a test user with credentials
/// </summary>
public class TestUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

