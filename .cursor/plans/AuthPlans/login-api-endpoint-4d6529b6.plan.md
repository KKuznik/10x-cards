<!-- 4d6529b6-f57a-4521-b41f-edfa0612bcc0 5232988c-5705-468f-b361-390b266e7c9c -->
# API Endpoint Implementation Plan: Login

## 1. Endpoint Overview

**Purpose:** Authenticate existing users and provide JWT tokens for subsequent API requests

- **HTTP Method:** POST
- **URL:** `/api/auth/login`
- **Authentication:** Not required (public endpoint)
- **User Story:** US-002 - User login functionality

## 2. Request Details

**HTTP Method:** POST

**URL Structure:** `/api/auth/login`

**Parameters:**

- Required: None (all data in request body)
- Optional: None

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Headers:**

- `Content-Type: application/json`

**Validation Rules:**

- `email`: Required, valid email format, max 255 characters
- `password`: Required (no format validation on login - only check existence)

## 3. Utilized Types

### Existing Types (No Changes Required)

**Request Models:**

- `LoginRequest` (`10xCards/Models/Requests/LoginRequest.cs`)
  - Properties: Email (string), Password (string)
  - Validation: DataAnnotations already applied

**Response Models:**

- `AuthResponse` (`10xCards/Models/Responses/AuthResponse.cs`)
  - Properties: UserId (Guid), Email (string), Token (string), ExpiresAt (DateTime)
- `ErrorResponse` (`10xCards/Models/Responses/ErrorResponse.cs`)
  - Properties: Message (string), ErrorCode (string), Errors (Dictionary)

**Common Models:**

- `Result<T>` (`10xCards/Models/Common/Result.cs`)
  - Generic wrapper for service operation results

### New Types Required

**None** - All required types already exist in the codebase

## 4. Response Details

### Success Response

**Status Code:** 200 OK

**Body:**

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-15T12:00:00Z"
}
```

**Headers:**

- `Content-Type: application/json`

### Error Responses

**401 Unauthorized - Invalid Credentials:**

```json
{
  "message": "Invalid email or password"
}
```

**400 Bad Request - Validation Error:**

```json
{
  "errors": {
    "email": ["Invalid email format"],
    "password": ["Password is required"]
  }
}
```

**500 Internal Server Error:**

```json
{
  "message": "An unexpected error occurred",
  "errorCode": "INTERNAL_ERROR"
}
```

## 5. Data Flow

### Login Flow

1. **Client Request** → POST /api/auth/login with email and password
2. **Endpoint Layer** → Validate request model (DataAnnotations)
3. **Service Layer** → AuthService.LoginUserAsync()

   - Find user by email using UserManager.FindByEmailAsync()
   - Verify password using UserManager.CheckPasswordAsync()
   - Generate JWT token with claims (userId, email, jti)
   - Calculate token expiration time

4. **Service Layer** → Return Result<AuthResponse> with success or failure
5. **Endpoint Layer** → Map result to HTTP response

   - Success: 200 OK with AuthResponse
   - Failure: 401 Unauthorized with error message

6. **Client Response** → Receive token and store securely (localStorage/cookie)

### Database Interactions

**Login:**

- Read operation on `AspNetUsers` table (via UserManager)
- Query: Find user by normalized email
- No write operations

## 6. Security Considerations

### Authentication

- No authentication required (public endpoint)
- Must validate credentials before issuing token
- Use ASP.NET Core Identity's built-in password hashing verification

### Data Validation

**Input Validation:**

- Email format validation via DataAnnotations
- Password presence validation (no format check on login)
- Model binding validation automatic via ASP.NET Core

**Output Sanitization:**

- Never return password hashes or sensitive data
- Error messages should be generic (avoid revealing if email exists)

### Security Threats and Mitigations

**Brute Force Attacks:**

- Threat: Automated password guessing
- Mitigation: Rate limiting (recommended for production, not in MVP)
- Consider: Account lockout after N failed attempts (ASP.NET Identity feature)

**Credential Stuffing:**

- Threat: Using leaked credentials from other services
- Mitigation: Monitor failed login patterns, consider CAPTCHA
- User education: Unique passwords, password managers

**Timing Attacks:**

- Threat: Analyzing response times to determine if email exists
- Mitigation: Constant-time password comparison (handled by Identity)
- Generic error messages ("Invalid email or password")

**Man-in-the-Middle (MITM):**

- Threat: Credential interception during transmission
- Mitigation: HTTPS required in production (enforced by UseHttpsRedirection)
- HSTS headers for strict transport security

**Token Storage:**

- Threat: XSS attacks accessing tokens in localStorage
- Mitigation: HttpOnly cookies (recommended) or secure storage practices
- Token expiration: 7 days (configurable via JwtSettings:ExpirationInMinutes)

### JWT Token Security

- **Algorithm:** HS256 (HMAC with SHA-256)
- **Secret Key:** Minimum 32 characters, stored in configuration
- **Claims:** userId, email, jti (unique token identifier)
- **Expiration:** Configurable (default: 10080 minutes = 7 days per API spec)
- **Validation:** Issuer, Audience, Lifetime, Signature

## 7. Error Handling

### Error Scenarios

| Scenario | Status Code | Error Response | Logging |

|----------|-------------|----------------|---------|

| Invalid email format | 400 Bad Request | Validation errors in errors object | Warning |

| Missing email or password | 400 Bad Request | Validation errors in errors object | Warning |

| User not found | 401 Unauthorized | "Invalid email or password" | Warning with email (hashed) |

| Incorrect password | 401 Unauthorized | "Invalid email or password" | Warning with userId |

| Database connection error | 500 Internal Server Error | Generic error message | Error with full exception |

| Unexpected exception | 500 Internal Server Error | Generic error message | Error with stack trace |

### Error Logging Strategy

**Security Audit Logging:**

- Log all failed login attempts with timestamp
- Include: Email attempted (hash for privacy), IP address, User-Agent
- Never log: Passwords, JWT tokens, password hashes

**Error Tracking:**

- Correlation IDs for request tracing (via GlobalExceptionHandlerMiddleware)
- Structured logging with ILogger
- Log levels: Warning (auth failures), Error (exceptions)

**Error Codes:**

- `INVALID_CREDENTIALS`: Wrong email or password
- `USER_NOT_FOUND`: Email doesn't exist (internal only, not exposed to client)
- `INTERNAL_ERROR`: Unexpected server errors

### Error Response Format

All errors follow the `ErrorResponse` model:

```json
{
  "message": "Human-readable error message",
  "errorCode": "MACHINE_READABLE_CODE",
  "errors": {
    "field": ["Specific validation error"]
  }
}
```

**Validation errors** use ASP.NET Core ValidationProblem format:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["Invalid email format"],
    "Password": ["Password is required"]
  }
}
```

## 8. Performance Considerations

### Database Queries

**Login:**

- Single query to find user by email (indexed on normalized email)
- Single query to verify password hash
- Expected latency: 10-50ms for database operations
- Connection pooling enabled by default (Npgsql)

### Optimization Strategies

**Query Optimization:**

- Email lookups use indexed column (AspNetUsers.NormalizedEmail)
- No N+1 queries (single user lookup)
- AsNoTracking() not needed (UserManager handles this)

**JWT Token Generation:**

- Token generation is CPU-bound (HMAC-SHA256)
- Expected latency: <5ms
- No external service calls
- Tokens cached client-side (no repeated generation for same session)

**Caching:**

- User lookups not cached (security - always verify latest state)
- JWT validation uses signature verification (fast)
- No external cache dependencies in MVP

### Scalability

**Throughput:**

- Expected: 500-1000 requests/second per server instance
- Bottleneck: Database connections (pool size: default 100)
- Horizontal scaling: Stateless design allows multiple instances

**Rate Limiting (Future Enhancement):**

- Recommended: 5 login attempts per minute per IP
- Recommended: 10 login attempts per hour per email
- Implementation: AspNetCoreRateLimit or custom middleware

### Performance Metrics

**Target SLAs:**

- P50: <100ms
- P95: <200ms
- P99: <500ms

**Monitoring:**

- Track failed login rates
- Track token generation time
- Track database query duration
- Alert on unusual login patterns

## 9. Implementation Steps

### Step 1: Add LoginUserAsync Method to IAuthService Interface

**File:** `10xCards/Services/IAuthService.cs`

Add new method signature:

```csharp
Task<Result<AuthResponse>> LoginUserAsync(LoginRequest request, CancellationToken cancellationToken = default);
```

### Step 2: Implement LoginUserAsync in AuthService

**File:** `10xCards/Services/AuthService.cs`

Implementation details:

1. Find user by email using `_userManager.FindByEmailAsync(request.Email)`
2. Early return if user not found (401 with generic message)
3. Verify password using `_userManager.CheckPasswordAsync(user, request.Password)`
4. Early return if password invalid (401 with generic message)
5. Generate JWT token using existing `GenerateJwtToken` method
6. Calculate expiration time
7. Log successful login
8. Return `Result<AuthResponse>.Success()`

Error handling:

- Log failed login attempts (Warning level)
- Return generic "Invalid email or password" message (no email enumeration)
- Never log passwords or tokens

### Step 3: Add Login Endpoint to AuthEndpoints

**File:** `10xCards/Endpoints/AuthEndpoints.cs`

Add inside `MapAuthEndpoints` method after the register endpoint:

```csharp
// POST /api/auth/login
group.MapPost("/login", async (
    [FromBody] LoginRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) => {
    
    var result = await authService.LoginUserAsync(request, cancellationToken);
    
    if (!result.IsSuccess) {
        return Results.Json(
            new ErrorResponse { Message = "Invalid email or password" },
            statusCode: StatusCodes.Status401Unauthorized
        );
    }
    
    return Results.Ok(result.Value);
})
.WithName("Login")
.WithSummary("Authenticate user")
.WithDescription("Validates user credentials and returns JWT token")
.Produces<AuthResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
.ProducesValidationProblem(StatusCodes.Status400BadRequest);
```

### Step 4: Test Login Endpoint Manually

**Testing Login with Valid Credentials:**

```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

Expected: 200 OK with AuthResponse containing JWT token

**Testing Login with Invalid Credentials:**

```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nonexistent@example.com",
    "password": "WrongPassword123!"
  }'
```

Expected: 401 Unauthorized with error message

**Testing Validation Errors:**

```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "invalid-email",
    "password": ""
  }'
```

Expected: 400 Bad Request with validation errors

### Step 5: Verify JWT Token Configuration

**File:** `appsettings.json` or `appsettings.Development.json`

Ensure the following configuration exists:

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters-long",
    "Issuer": "10xCards",
    "Audience": "10xCards",
    "ExpirationInMinutes": "10080"
  }
}
```

**Note:** ExpirationInMinutes: 10080 = 7 days (per API spec)

### Step 6: Add Integration Tests (Optional for MVP)

**New file:** `10xCards.Tests/Endpoints/LoginEndpointTests.cs`

Test scenarios:

- Login with valid credentials returns 200 OK with AuthResponse
- Login with invalid email returns 401 Unauthorized
- Login with invalid password returns 401 Unauthorized
- Login with invalid email format returns 400 Bad Request
- Login with missing email returns 400 Bad Request
- Login with missing password returns 400 Bad Request
- Verify JWT token contains correct claims

---

## Additional Notes

### Future Enhancements

1. **Rate Limiting:** Add rate limiting middleware to prevent brute force attacks
2. **Account Lockout:** Enable ASP.NET Identity lockout after N failed attempts
3. **Two-Factor Authentication:** Add 2FA support for enhanced security
4. **Audit Logging:** Create audit log table for login events with IP and User-Agent
5. **Refresh Tokens:** Add refresh token mechanism for seamless token renewal
6. **Remember Me:** Add option to extend token expiration for trusted devices

### Security Checklist

- [ ] HTTPS enforced in production
- [ ] JWT secret key stored in secure configuration (Azure Key Vault in production)
- [ ] Passwords hashed with Identity's default hasher (PBKDF2)
- [ ] Generic error messages prevent email enumeration
- [ ] Failed login attempts logged for security monitoring
- [ ] JWT tokens expire after 7 days (per API spec)
- [ ] ClockSkew set to TimeSpan.Zero for strict expiration
- [ ] No sensitive data logged (passwords, tokens, hashes)

### Configuration Requirements

Verify that the following components are already configured in `Program.cs`:

1. ASP.NET Core Identity with password requirements
2. JWT Authentication with Bearer scheme
3. Token validation parameters (issuer, audience, signing key)
4. Authentication and Authorization middleware
5. IAuthService registered in DI container

All of these are already present based on the existing codebase analysis.