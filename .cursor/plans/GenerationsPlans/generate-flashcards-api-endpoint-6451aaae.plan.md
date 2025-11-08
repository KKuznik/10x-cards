<!-- 6451aaae-6575-4322-9438-e83f6cc036fc 02f22592-192d-43bc-ae9b-277448b33425 -->
# API Endpoint Implementation Plan: Generate Flashcards (POST /api/generations)

## 1. Endpoint Overview

**Purpose:** Generate flashcard suggestions from user-provided text using AI models via OpenRouter API. The endpoint validates input, calls the AI service, stores generation metadata, and returns proposed flashcards without saving them to the database.

**HTTP Method:** POST

**Route:** `/api/generations`

**Authentication:** Required (JWT Bearer token)

**Authorization:** User can only generate flashcards for their own account

## 2. Request Details

### HTTP Structure

- **Method:** POST
- **URL Pattern:** `/api/generations`
- **Content-Type:** `application/json`
- **Authorization Header:** `Bearer {jwt_token}`

### Parameters

**Required (Request Body):**

- `sourceText` (string): Text to generate flashcards from
  - Min length: 1000 characters
  - Max length: 10000 characters
- `model` (string): OpenRouter model identifier (e.g., "openai/gpt-4o-mini")
  - Max length: 100 characters

**No Optional Parameters**

### Request Body Example

```json
{
  "sourceText": "Photosynthesis is the process...",
  "model": "openai/gpt-4o-mini"
}
```

## 3. Utilized Types

### Existing Models

- **Request:** `GenerateFlashcardsRequest` (10xCards/Models/Requests/GenerateFlashcardsRequest.cs)
- **Response:** `GenerationResponse` (10xCards/Models/Responses/GenerationResponse.cs)
- **DTO:** `ProposedFlashcardDto` (10xCards/Models/Responses/ProposedFlashcardDto.cs)
- **Entity:** `Generation` (10xCards/Database/Entities/Generation.cs)
- **Entity:** `GenerationErrorLog` (10xCards/Database/Entities/GenerationErrorLog.cs)
- **Common:** `Result<T>` (10xCards/Models/Common/Result.cs)

### New Models Needed

**None** - all necessary models already exist

## 4. Response Details

### Success Response (201 Created)

```json
{
  "id": 5,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "model": "openai/gpt-4o-mini",
  "generatedCount": 8,
  "acceptedUneditedCount": null,
  "acceptedEditedCount": null,
  "sourceTextHash": "a1b2c3d4e5f6...",
  "sourceTextLength": 2450,
  "generationDuration": 3200,
  "createdAt": "2025-11-03T10:00:00Z",
  "updatedAt": "2025-11-03T10:00:00Z",
  "flashcards": [
    {
      "front": "What is photosynthesis?",
      "back": "The process by which plants convert light energy..."
    }
  ]
}
```

### Error Responses

**400 Bad Request - Validation Failed**

```json
{
  "errors": {
    "sourceText": ["Source text must be between 1000 and 10000 characters"],
    "model": ["Model is required"]
  }
}
```

**401 Unauthorized - Missing/Invalid Token**

```json
{
  "message": "Unauthorized"
}
```

**500 Internal Server Error - AI Generation Failed**

```json
{
  "message": "Failed to generate flashcards",
  "errorCode": "AI_GENERATION_ERROR"
}
```

**503 Service Unavailable - AI Service Down**

```json
{
  "message": "AI service temporarily unavailable",
  "errorCode": "AI_SERVICE_UNAVAILABLE"
}
```

## 5. Data Flow

### High-Level Flow

1. **Request Reception** → Endpoint receives POST request with sourceText and model
2. **Authentication** → Validate JWT token and extract userId
3. **Validation** → Validate request body (DataAnnotations)
4. **Hash Calculation** → Calculate SHA-256 hash of sourceText
5. **AI Service Call** → Call OpenRouter API via IOpenRouterService
6. **Duration Measurement** → Track generation time in milliseconds
7. **Database Storage** → Save Generation record with metadata
8. **Error Handling** → If AI fails, log to generation_error_logs
9. **Response Mapping** → Map to GenerationResponse and return 201

### Detailed Component Interaction

```
HTTP Request
    ↓
GenerationEndpoints.MapPost()
    ↓ (extract userId from JWT)
    ↓ (validate request)
    ↓
IGenerationService.GenerateFlashcardsAsync()
    ↓
    ├─→ Calculate SHA-256 hash (using System.Security.Cryptography)
    ├─→ Start stopwatch for duration tracking
    ├─→ IOpenRouterService.GenerateFlashcardsAsync()
    │       ↓
    │       ├─→ HttpClient request to OpenRouter API
    │       ├─→ Parse JSON response
    │       └─→ Return List<ProposedFlashcardDto> or throw exception
    ├─→ Stop stopwatch
    ├─→ Create Generation entity
    ├─→ Save to database via ApplicationDbContext
    └─→ Return Result<GenerationResponse>
    
If Error:
    ├─→ Create GenerationErrorLog entity
    ├─→ Save error to database
    └─→ Return Result.Failure()
```

### External Service Integration

**OpenRouter API:**

- Base URL: `https://openrouter.ai/api/v1/chat/completions`
- Authentication: Bearer token (API key from environment)
- Request format: OpenAI-compatible chat completions
- Response parsing: JSON with structured flashcard data
- Timeout: 30 seconds
- Retry logic: None (fail fast)

## 6. Security Considerations

### Authentication & Authorization

- **JWT Validation:** Handled by ASP.NET Core authentication middleware
- **User Extraction:** ClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)
- **Row-Level Security:** Generation record automatically associated with authenticated userId
- **Authorization Check:** No cross-user access possible (userId enforced)

### Input Validation

- **Data Annotations:** Automatic validation via ASP.NET Core model binding
- **Length Constraints:** Prevent abuse with 1000-10000 character limit
- **Model Validation:** Ensure valid OpenRouter model identifier format

### API Key Security

- **Storage:** Environment variable `OPENROUTER_API_KEY`
- **Access:** Only IOpenRouterService accesses the key
- **Never Logged:** Ensure API key never appears in logs or responses

### Data Privacy

- **No Source Text Storage:** Only hash and length stored
- **Hash Algorithm:** SHA-256 (one-way, secure)
- **Purpose:** Deduplication tracking only

### Potential Threats

- **Rate Limiting:** Consider implementing per-user rate limits (future)
- **Cost Control:** Monitor OpenRouter API usage and costs
- **Malicious Input:** Validate and sanitize all inputs
- **API Key Exposure:** Ensure key is never committed to source control

## 7. Error Handling

### Error Hierarchy

#### 1. Validation Errors (400 Bad Request)

**Trigger:** Invalid request body (DataAnnotations validation fails)

**Response:** ProblemDetails with validation errors

**Action:** Return immediately, no service call

#### 2. Authentication Errors (401 Unauthorized)

**Trigger:** Missing/invalid JWT token or userId extraction fails

**Response:** Unauthorized result

**Action:** Return immediately

#### 3. AI Service Errors (500/503)

**Trigger:** OpenRouter API call fails

**Subtypes:**

- **Timeout (503):** Request exceeds 30 seconds
- **Rate Limit (503):** 429 from OpenRouter
- **Invalid Response (500):** Malformed JSON or unexpected structure
- **API Error (500):** 4xx/5xx from OpenRouter

**Action:**

1. Log error with structured logging (ILogger)
2. Create GenerationErrorLog record in database
3. Return user-friendly error message
4. Include error code for client-side handling

**Error Codes:**

- `AI_TIMEOUT`: Request timeout
- `AI_RATE_LIMIT`: Rate limit exceeded
- `AI_INVALID_RESPONSE`: Cannot parse response
- `AI_API_ERROR`: Generic API error
- `AI_SERVICE_UNAVAILABLE`: Service down

#### 4. Database Errors (500)

**Trigger:** Database operation fails (SaveChangesAsync)

**Response:** Internal Server Error

**Action:**

1. Log error with full exception details
2. Return generic error message (don't expose DB details)

### Error Logging Strategy

**Structured Logging (ILogger):**

```csharp
_logger.LogError(ex, 
    "AI generation failed for user {UserId}, model {Model}, textLength {TextLength}",
    userId, model, sourceTextLength);
```

**Database Error Log (generation_error_logs):**

```csharp
var errorLog = new GenerationErrorLog {
    UserId = userId,
    Model = model,
    SourceTextHash = hash,
    SourceTextLength = sourceTextLength,
    ErrorCode = "AI_TIMEOUT",
    ErrorMessage = ex.Message,
    CreatedAt = DateTime.UtcNow
};
await _context.GenerationErrorLogs.AddAsync(errorLog);
await _context.SaveChangesAsync();
```

### Guard Clauses Pattern

Following clean code practices, implement early returns:

1. Validate userId (empty Guid check)
2. Validate request model (DataAnnotations)
3. Check AI service availability (health check)
4. Handle AI exceptions with try-catch
5. Place happy path last

## 8. Performance Considerations

### Potential Bottlenecks

1. **AI API Latency:** OpenRouter calls may take 3-10 seconds
2. **Database Writes:** Two potential writes (Generation + GenerationErrorLog)
3. **Hash Calculation:** SHA-256 on 10KB text (~1ms, negligible)

### Optimization Strategies

#### 1. Asynchronous Operations

- Use async/await throughout the pipeline
- HttpClient.SendAsync for non-blocking AI calls
- SaveChangesAsync for non-blocking DB writes

#### 2. Connection Pooling

- Entity Framework Core handles DB connection pooling
- HttpClient registered as singleton with dependency injection

#### 3. Timeouts

- Set reasonable timeout for OpenRouter API (30 seconds)
- Use CancellationToken for request cancellation support

#### 4. Response Streaming (Future)

- Consider Server-Sent Events for real-time flashcard streaming
- Not in MVP scope

#### 5. Caching (Not Applicable)

- Each generation is unique (user-specific text)
- No caching opportunity for this endpoint

### Resource Management

- **HttpClient:** Singleton, properly disposed by DI container
- **DbContext:** Scoped lifetime, auto-disposed
- **CancellationToken:** Passed through entire pipeline

### Monitoring Metrics

- **Generation Duration:** Already tracked in database
- **Success Rate:** Calculate from Generations vs GenerationErrorLogs
- **Average Response Time:** Monitor with application insights
- **AI Cost Tracking:** Log token usage from OpenRouter response

## 9. Implementation Steps

### Step 1: Create OpenRouter AI Service Interface

**File:** `10xCards/Services/IOpenRouterService.cs`

Define interface with:

- `GenerateFlashcardsAsync(string sourceText, string model, CancellationToken)` method
- Returns `Task<List<ProposedFlashcardDto>>`
- Throws exceptions on failure (handled by caller)

### Step 2: Implement OpenRouter AI Service

**File:** `10xCards/Services/OpenRouterService.cs`

Implementation details:

- Inject `IHttpClientFactory` and `IConfiguration`
- Read API key from configuration (`OPENROUTER_API_KEY`)
- Construct OpenRouter API request (OpenAI-compatible format)
- Include system prompt for flashcard generation
- Parse JSON response and extract flashcard pairs
- Handle HTTP errors, timeouts, and invalid responses
- Use structured logging with ILogger

### Step 3: Register OpenRouter Service in DI Container

**File:** `10xCards/Program.cs`

Add service registration:

```csharp
builder.Services.AddScoped<IOpenRouterService, OpenRouterService>();
```

Configure HttpClient:

```csharp
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>(client => {
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Step 4: Extend IGenerationService Interface

**File:** `10xCards/Services/IGenerationService.cs`

Add method:

```csharp
Task<Result<GenerationResponse>> GenerateFlashcardsAsync(
    Guid userId,
    GenerateFlashcardsRequest request,
    CancellationToken cancellationToken = default);
```

### Step 5: Implement GenerateFlashcardsAsync in GenerationService

**File:** `10xCards/Services/GenerationService.cs`

Implementation steps:

1. Guard clause: Validate userId
2. Calculate SHA-256 hash of sourceText
3. Start stopwatch for duration measurement
4. Try-catch block for AI service call
5. Call `_openRouterService.GenerateFlashcardsAsync()`
6. Stop stopwatch and calculate duration in milliseconds
7. Create Generation entity with all fields
8. Save to database via `_context.Generations.AddAsync()`
9. Commit with `_context.SaveChangesAsync()`
10. Map entity to GenerationResponse
11. Return `Result<GenerationResponse>.Success(response)`
12. On exception: Log error, create GenerationErrorLog, return `Result.Failure()`

### Step 6: Add POST Endpoint to GenerationEndpoints

**File:** `10xCards/Endpoints/GenerationEndpoints.cs`

Add mapping in `MapGenerationEndpoints()`:

```csharp
group.MapPost("", async (
    [FromBody] GenerateFlashcardsRequest request,
    IGenerationService generationService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) => {
    
    // Extract userId from JWT
    // Call generationService.GenerateFlashcardsAsync()
    // Return Results.Created() with 201 status
    // Handle errors appropriately
})
.WithName("GenerateFlashcards")
.WithSummary("Generate flashcard suggestions using AI")
.Produces<GenerationResponse>(StatusCodes.Status201Created)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);
```

### Step 7: Add Configuration for OpenRouter API Key

**File:** `appsettings.json` or Environment Variables

Add configuration:

```json
{
  "OpenRouter": {
    "ApiKey": "sk-or-v1-..."
  }
}
```

Or set environment variable: `OPENROUTER_API_KEY`

### Step 8: Create Helper Method for SHA-256 Hashing

**Option A:** Static utility class

**Option B:** Extension method on string

**Option C:** Private method in GenerationService

Implement using `System.Security.Cryptography.SHA256`:

```csharp
private static string CalculateSha256Hash(string input) {
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}
```

### Step 9: Test the Endpoint

**Create test file:** `10xCards/EndpointsTests/GenerationTests/test-post-generate-flashcards.http`

Test scenarios:

1. Valid request with valid text and model
2. Invalid request (text too short)
3. Invalid request (text too long)
4. Invalid request (missing model)
5. Unauthorized request (no token)
6. AI service error simulation

### Step 10: Update API Documentation

**File:** `.ai/api-plan.md`

Update implementation status:

- Mark POST /api/generations as "Implemented"
- Document any deviations from original spec
- Add notes about OpenRouter integration

### To-dos

- [ ] Create IOpenRouterService interface in Services folder
- [ ] Implement OpenRouterService with AI API integration
- [ ] Register OpenRouter service and HttpClient in Program.cs
- [ ] Add GenerateFlashcardsAsync method to IGenerationService
- [ ] Implement GenerateFlashcardsAsync in GenerationService
- [ ] Add POST /api/generations endpoint in GenerationEndpoints
- [ ] Add OpenRouter API key configuration
- [ ] Create HTTP test file for the endpoint