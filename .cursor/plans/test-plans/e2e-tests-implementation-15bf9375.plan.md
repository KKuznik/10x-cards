<!-- 15bf9375-89be-4ab2-872f-34fa380e4e7f b02ac5e6-c649-41c1-a49f-1bfc8ae11cd9 -->
# E2E Tests Implementation with Playwright and Testcontainers

## Overview

Create a new E2E test project using Playwright for browser automation and Testcontainers for PostgreSQL database isolation. Focus on login and registration scenarios with unique test data per test.

## Implementation Steps

### 1. Create Test Project Structure

- Create `10xCards.E2ETests` directory and project file
- Add NuGet packages:
- `Microsoft.Playwright.NUnit` (or xUnit if preferred)
- `Testcontainers.PostgreSql`
- `Microsoft.AspNetCore.Mvc.Testing`
- `xunit` and `xunit.runner.visualstudio`
- Reference the main `10xCards` project

### 2. Setup Test Infrastructure

**DatabaseFixture.cs** - Testcontainer management

- Create singleton PostgreSQL container using `Testcontainers.PostgreSql`
- Container lifecycle: start once for all tests, dispose at end
- Expose connection string for the web application
- Handle database migrations

**WebApplicationFactory** - Application hosting

- Create custom `WebApplicationFactory<Program>` 
- Override `ConfigureWebHost` to use Testcontainer connection string
- Ensure JWT settings and other configurations are set for testing
- Start application on a random available port

**PlaywrightFixture.cs** - Browser management

- Install Playwright browsers (run `pwsh bin/Debug/net9.0/playwright.ps1 install`)
- Create and manage browser context
- Provide page instances for tests
- Configure browser to use test application URL

### 3. Test Helper Utilities

**TestDataGenerator.cs**

- Generate unique emails using `Guid.NewGuid()` (e.g., `user_{guid}@test.com`)
- Generate valid passwords meeting requirements (min 8 chars, upper, lower, digit, special)
- Helper methods to create test user data on-demand

### 4. Test Implementation

**RegisterTests.cs**

- Test successful registration with unique credentials
- Test registration with existing email (should fail)
- Test registration with invalid password (missing requirements)
- Test registration with mismatched passwords

**LoginTests.cs**

- Test successful login with registered user
- Test login with invalid credentials
- Test login with non-existent user
- Test navigation after successful login

Each test:

1. Generate unique user data
2. Navigate to page
3. Fill form fields using Playwright selectors
4. Submit form
5. Assert expected outcome (success message, error, navigation)

### 5. Key Files to Create

- `10xCards.E2ETests/10xCards.E2ETests.csproj`
- `10xCards.E2ETests/Infrastructure/DatabaseFixture.cs`
- `10xCards.E2ETests/Infrastructure/CustomWebApplicationFactory.cs`
- `10xCards.E2ETests/Infrastructure/PlaywrightFixture.cs`
- `10xCards.E2ETests/Helpers/TestDataGenerator.cs`
- `10xCards.E2ETests/Tests/RegisterTests.cs`
- `10xCards.E2ETests/Tests/LoginTests.cs`
- `10xCards.E2ETests/GlobalUsings.cs` (common imports)

### 6. Build and Verify

- Update `10xCards.sln` to include the new project
- Run `dotnet build` from solution directory
- Execute one test (e.g., successful registration test) to verify setup

## Technical Details

**Database Strategy:**

- Single PostgreSQL container shared across all tests
- No database cleanup between tests (relies on unique data per test)
- Connection string injected into WebApplicationFactory

**Data Isolation:**

- Each test generates unique email addresses
- No interference between parallel tests
- Example: `$"test_{Guid.NewGuid()}@example.com"`

**Playwright Selectors:**

- Use `id` selectors for form fields (e.g., `#email`, `#password`)
- Use button text for submit buttons
- Check for alert messages or navigation for assertions

**Program.cs Modification:**

- May need to make `Program` class partial and public for `WebApplicationFactory<Program>`

### To-dos

- [ ] Create E2E test project with required NuGet packages
- [ ] Implement Testcontainers PostgreSQL fixture
- [ ] Create custom WebApplicationFactory for test hosting
- [ ] Setup Playwright fixture and browser management
- [ ] Create test data generator utilities
- [ ] Implement registration E2E tests
- [ ] Implement login E2E tests
- [ ] Add E2E project to solution and verify build
- [ ] Execute one test to verify complete setup