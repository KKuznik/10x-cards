<!-- e3cc9731-6e7b-4340-9998-ee1eea7cd10c d44929d4-d07a-48fb-96fe-7a456288d6ff -->
# API Endpoint Implementation Plan: Update Flashcard

## 1. Endpoint Overview

The Update Flashcard endpoint allows authenticated users to modify the front and back content of their existing flashcards. When a flashcard originally created by AI (source: 'ai-full') is edited, the system automatically changes its source to 'ai-edited' to track modifications. This supports User Story US-005.

**Key Features:**

- Update flashcard front and back content
- Automatic source tracking (ai-full → ai-edited)
- Row-level security (users can only update their own flashcards)
- Preserve generation relationship if exists

## 2. Request Details

- **HTTP Method:** PUT
- **URL Structure:** `/api/flashcards/{id}`
- **Authentication:** Required (JWT Bearer token)

### Parameters:

**Route Parameters:**

- `id` (long, required) - The unique identifier of the flashcard to update

**Request Body:**

```json
{
  "front": "What is the capital of France?",
  "back": "Paris, located in the Île-de-France region"
}
```

**Headers:**

- `Authorization: Bearer {jwt_token}` (required)
- `Content-Type: application/json` (required)

## 3. Utilized Types

### Request Model (Existing)

**File:** `10xCards/Models/Requests/UpdateFlashcardRequest.cs`

Already implemented with validation attributes:

- `Front` (string): Required, 1-200 characters
- `Back` (string): Required, 1-500 characters

### Response Model (Existing)

**File:** `10xCards/Models/Responses/FlashcardResponse.cs`

Returns complete flashcard data including:

- `Id`, `Front`, `Back`, `Source`, `CreatedAt`, `UpdatedAt`, `GenerationId`

### Entity (Existing)

**File:** `10xCards/Database/Entities/Flashcard.cs`

Database entity with all flashcard properties.

## 4. Response Details

### Success Response (200 OK)

```json
{
  "id": 1,
  "front": "What is the capital of France?",
  "back": "Paris, located in the Île-de-France region",
  "source": "ai-edited",
  "createdAt": "2025-11-03T10:00:00Z",
  "updatedAt": "2025-11-03T11:30:00Z",
  "generationId": 5
}
```

### Error Responses

**400 Bad Request - Validation Failed**

```json
{
  "message": "Validation failed",
  "errors": {
    "Front": ["Front must not exceed 200 characters"],
    "Back": ["Back is required"]
  }
}
```

**401 Unauthorized - Missing or Invalid Token**

```json
{
  "message": "Unauthorized"
}
```

**404 Not Found - Flashcard Not Found**

```json
{
  "message": "Flashcard not found or does not belong to user"
}
```

**500 Internal Server Error - Database Error**

```json
{
  "message": "An error occurred while updating the flashcard"
}
```

## 5. Data Flow

1. **Request Reception**

   - Endpoint receives PUT request with flashcard ID in route
   - Extract user ID from JWT claims (ClaimTypes.NameIdentifier)
   - Validate JWT token and extract userId

2. **Service Layer Processing**

   - Call `IFlashcardService.UpdateFlashcardAsync(userId, id, request, cancellationToken)`
   - Query flashcard with tracking: `_context.Flashcards.Where(f => f.Id == id && f.UserId == userId)`
   - Verify ownership (row-level security)

3. **Business Logic**

   - Trim input strings for `front` and `back`
   - If current source is 'ai-full', update to 'ai-edited'
   - If current source is 'ai-edited' or 'manual', keep unchanged
   - Preserve `generationId` if exists
   - Update entity properties
   - Set `UpdatedAt` (auto-handled by database trigger)

4. **Database Persistence**

   - Save changes using `_context.SaveChangesAsync()`
   - Database trigger automatically updates `updated_at` column

5. **Response Mapping**

   - Map updated entity to `FlashcardResponse` DTO
   - Return Result<FlashcardResponse> with success flag

6. **Endpoint Response**

   - Return 200 OK with flashcard data
   - Or appropriate error status code

## 6. Security Considerations

### Authentication

- JWT Bearer token validation via ASP.NET Core authentication middleware
- Token must be valid and not expired
- Extract userId from ClaimTypes.NameIdentifier claim

### Authorization (Row-Level Security)

- Users can only update flashcards where `flashcard.UserId == authenticatedUserId`
- Query filter: `WHERE id = {id} AND user_id = {userId}`
- Return 404 if flashcard doesn't exist OR doesn't belong to user (don't reveal existence)

### Input Validation

- Use DataAnnotations on `UpdateFlashcardRequest` for validation
- Framework automatically validates before controller logic
- Trim whitespace to prevent injection attacks
- MaxLength constraints prevent database overflow

### Data Integrity

- Use Entity Framework Core parameterized queries (prevents SQL injection)
- Database constraints enforce data integrity (CHECK constraint on source)
- Preserve referential integrity (generationId relationship)

## 7. Error Handling

### Validation Errors (400 Bad Request)

- **Trigger:** Invalid request body (empty fields, exceeds max length)
- **Detection:** Model validation via DataAnnotations
- **Response:** Return validation problem details with field-level errors
- **Logging:** Warning level

### Authentication Errors (401 Unauthorized)

- **Trigger:** Missing token, invalid token, expired token, malformed userId claim
- **Detection:** Token validation middleware, ClaimsPrincipal parsing
- **Response:** Return 401 Unauthorized
- **Logging:** Warning level with userId attempt

### Not Found Errors (404 Not Found)

- **Trigger:** Flashcard doesn't exist OR doesn't belong to user
- **Detection:** Query returns null from `FirstOrDefaultAsync()`
- **Response:** Return 404 with generic message (don't reveal if exists)
- **Logging:** Warning level with userId and flashcardId
- **Security Note:** Don't distinguish between "not found" and "not yours" to prevent information disclosure

### Database Errors (500 Internal Server Error)

- **Trigger:** Database connection issues, constraint violations, unexpected DB errors
- **Detection:** Catch `DbUpdateException` and general `Exception`
- **Response:** Return 500 with generic error message
- **Logging:** Error level with full exception details and context

### Edge Cases

- **Empty trim result:** Validation should catch this (MinLength(1))
- **Concurrent updates:** Last write wins (optimistic concurrency not required for MVP)
- **Large payload:** Framework automatically limits request size

## 8. Performance Considerations

### Database Optimization

- Use indexed query on `id` and `user_id` (compound index benefits)
- Query with tracking since entity will be modified
- Single database roundtrip for query + update
- Database trigger efficiently handles `updated_at` update

### Caching Strategy

- Not applicable for update operations (writes invalidate cache)
- Consider cache invalidation if read caching implemented later

### Expected Performance

- **Query time:** < 10ms (indexed lookup)
- **Update time:** < 20ms (simple update with trigger)
- **Total response time:** < 50ms under normal load

## 9. Implementation Steps

### Step 1: Extend Service Interface

**File:** `10xCards/Services/IFlashcardService.cs`

Add method signature:

```csharp
/// <summary>
/// Updates an existing flashcard for a specific user
/// </summary>
/// <param name="userId">The ID of the user updating the flashcard</param>
/// <param name="flashcardId">The ID of the flashcard to update</param>
/// <param name="request">The flashcard update request</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result containing the updated flashcard</returns>
Task<Result<FlashcardResponse>> UpdateFlashcardAsync(
    Guid userId,
    long flashcardId,
    UpdateFlashcardRequest request,
    CancellationToken cancellationToken = default);
```

### Step 2: Implement Service Method

**File:** `10xCards/Services/FlashcardService.cs`

Implement the method with:

1. Guard clauses for userId and request validation
2. Query flashcard with tracking: `_context.Flashcards.Where(f => f.Id == flashcardId && f.UserId == userId).FirstOrDefaultAsync()`
3. Return 404 error if flashcard is null
4. Trim input strings
5. Apply source logic: if `flashcard.Source == "ai-full"`, set to `"ai-edited"`
6. Update `Front` and `Back` properties
7. Save changes: `await _context.SaveChangesAsync(cancellationToken)`
8. Map to `FlashcardResponse` and return success result
9. Catch `DbUpdateException` and general `Exception` with appropriate logging

### Step 3: Create API Endpoint

**File:** `10xCards/Endpoints/FlashcardEndpoints.cs`

Add endpoint mapping inside `MapFlashcardEndpoints` method:

```csharp
// PUT /api/flashcards/{id}
group.MapPut("/{id:long}", async (
    long id,
    [FromBody] UpdateFlashcardRequest request,
    IFlashcardService flashcardService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    // Extract userId from JWT claims
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var result = await flashcardService.UpdateFlashcardAsync(userId, id, request, cancellationToken);

    if (!result.IsSuccess)
    {
        // Check for "not found" error for 404 response
        if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Results.NotFound(new { message = result.ErrorMessage });
        }
        return Results.BadRequest(new { message = result.ErrorMessage });
    }

    return Results.Ok(result.Value);
})
.WithName("UpdateFlashcard")
.WithSummary("Update an existing flashcard")
.WithDescription("Updates the front and back content of a flashcard. AI-generated flashcards are marked as edited.")
.Produces<FlashcardResponse>(StatusCodes.Status200OK)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);
```

### Step 4: Testing

Create test file: `10xCards/EndpointsTests/test-update-flashcard-endpoint.http`

Test scenarios:

1. **Success case** - Update existing flashcard (200 OK)
2. **Update AI flashcard** - Verify source changes from 'ai-full' to 'ai-edited'
3. **Validation errors** - Empty fields, exceeds max length (400 Bad Request)
4. **Unauthorized** - No token, invalid token (401 Unauthorized)
5. **Not found** - Non-existent flashcard ID (404 Not Found)
6. **Forbidden** - Attempt to update another user's flashcard (404 Not Found)
7. **Edge cases** - Whitespace trimming, special characters

### Step 5: Verification

1. Run application and execute HTTP tests
2. Verify source change logic (ai-full → ai-edited)
3. Verify row-level security (can't update other users' flashcards)
4. Check database to confirm `updated_at` trigger works
5. Verify all error responses return correct status codes
6. Review logs for appropriate logging levels

---

## Dependencies

- Existing `UpdateFlashcardRequest` model (already implemented)
- Existing `FlashcardResponse` model (already implemented)
- Existing `Flashcard` entity (already implemented)
- Existing `IFlashcardService` interface (extend)
- Existing `FlashcardService` class (extend)
- Existing `FlashcardEndpoints` class (extend)
- Database trigger for `updated_at` (already implemented)

### To-dos

- [ ] Add UpdateFlashcardAsync method signature to IFlashcardService interface
- [ ] Implement UpdateFlashcardAsync in FlashcardService with business logic and source tracking
- [ ] Add PUT /api/flashcards/{id} endpoint mapping in FlashcardEndpoints
- [ ] Create comprehensive HTTP test file for update flashcard endpoint
- [ ] Test all scenarios including source change logic and row-level security