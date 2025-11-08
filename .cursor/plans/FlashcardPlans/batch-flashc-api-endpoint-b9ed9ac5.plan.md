<!-- b9ed9ac5-05fb-4e29-8c66-561884cc841a b598ce12-6662-4e27-922e-c5331ea709e9 -->
# API Endpoint Implementation Plan: Create Multiple Flashcards from Generation

## 1. Endpoint Overview

The `POST /api/flashcards/batch` endpoint allows authenticated users to create multiple flashcards in a single request from an AI generation session. This endpoint is designed to handle bulk flashcard creation from the AI generation feature (US-004), where users can accept multiple generated flashcards (either unedited or edited) at once.

**Key Characteristics:**

- Accepts 1-50 flashcards per request
- Associates flashcards with a specific generation record
- Updates generation statistics (accepted counts) atomically
- Validates generation ownership to prevent unauthorized access
- Uses transactions to ensure data consistency

## 2. Request Details

**HTTP Method:** POST

**URL Structure:** `/api/flashcards/batch`

**Authentication:** Required - JWT Bearer token

**Request Headers:**

- `Authorization: Bearer {token}`
- `Content-Type: application/json`

**Request Body Structure:**

```json
{
  "generationId": 5,
  "flashcards": [
    {
      "front": "What is photosynthesis?",
      "back": "The process by which plants convert light energy into chemical energy",
      "source": "ai-full"
    },
    {
      "front": "What pigment absorbs light?",
      "back": "Chlorophyll, primarily found in chloroplasts",
      "source": "ai-edited"
    }
  ]
}
```

**Parameters:**

**Required:**

- `generationId` (long): Positive integer referencing existing generation owned by user
- `flashcards` (array): Collection of flashcard items
  - Minimum: 1 item
  - Maximum: 50 items
  - Each flashcard item contains:
    - `front` (string): Question/prompt text (1-200 characters, required)
    - `back` (string): Answer text (1-500 characters, required)
    - `source` (string): Must be 'ai-full' or 'ai-edited' (required)

**Optional:** None

## 3. Types Used

**Request Models:**

- `CreateFlashcardsBatchRequest` - Main request model at `10xCards/Models/Requests/CreateFlashcardsBatchRequest.cs`
  - Contains validation: Required, Range(1, long.MaxValue) for generationId
  - Contains validation: Required, MinLength(1), MaxLength(50) for flashcards array

- `BatchFlashcardItem` - Individual flashcard at `10xCards/Models/Requests/BatchFlashcardItem.cs`
  - Contains validation: Required, MinLength(1), MaxLength(200) for Front
  - Contains validation: Required, MinLength(1), MaxLength(500) for Back
  - Contains validation: Required, RegularExpression for Source

**Response Models:**

- `CreateFlashcardsBatchResponse` - Response at `10xCards/Models/Responses/CreateFlashcardsBatchResponse.cs`
  - Properties: Created (int), Flashcards (List<FlashcardResponse>)

- `FlashcardResponse` - Individual flashcard response at `10xCards/Models/Responses/FlashcardResponse.cs`
  - Properties: Id, Front, Back, Source, CreatedAt, UpdatedAt, GenerationId

**Service Models:**

- `Result<CreateFlashcardsBatchResponse>` - Wrapper for service operation results

**Entity Models:**

- `Flashcard` - Database entity at `10xCards/Database/Entities/Flashcard.cs`
- `Generation` - Database entity at `10xCards/Database/Entities/Generation.cs`

## 4. Response Details

**Success Response (201 Created):**

```json
{
  "created": 2,
  "flashcards": [
    {
      "id": 10,
      "front": "What is photosynthesis?",
      "back": "The process by which plants convert light energy into chemical energy",
      "source": "ai-full",
      "createdAt": "2025-11-03T10:00:00Z",
      "updatedAt": "2025-11-03T10:00:00Z",
      "generationId": 5
    },
    {
      "id": 11,
      "front": "What pigment absorbs light?",
      "back": "Chlorophyll, primarily found in chloroplasts",
      "source": "ai-edited",
      "createdAt": "2025-11-03T10:00:00Z",
      "updatedAt": "2025-11-03T10:00:00Z",
      "generationId": 5
    }
  ]
}
```

**Error Responses:**

**400 Bad Request - Validation Errors:**

```json
{
  "errors": {
    "flashcards[0].front": ["Front must not exceed 200 characters"],
    "flashcards[1].back": ["Back is required"]
  }
}
```

**400 Bad Request - Business Logic Error:**

```json
{
  "message": "Invalid generation ID"
}
```

**401 Unauthorized:**

No body, standard HTTP 401 status

**404 Not Found:**

```json
{
  "message": "Generation not found or does not belong to user"
}
```

**500 Internal Server Error:**

```json
{
  "message": "An unexpected error occurred while creating flashcards"
}
```

## 5. Data Flow

### High-Level Flow:

1. **API Layer** receives POST request to `/api/flashcards/batch`
2. **Model Binding** validates request structure using DataAnnotations
3. **Authentication** extracts userId from JWT claims
4. **Service Layer** executes business logic:

   - Validates generation exists and belongs to user
   - Creates flashcard entities
   - Calculates acceptance counts
   - Updates generation statistics
   - Saves all changes in single transaction

5. **Response Mapping** converts entities to DTOs
6. **API Layer** returns 201 Created with response body

### Detailed Service Flow:

```
CreateFlashcardsBatchAsync(userId, request)
│
├─ Guard Clause: Validate userId != Guid.Empty
│  └─ Return Failure if invalid
│
├─ Guard Clause: Validate request != null
│  └─ Return Failure if null
│
├─ Query Generation from database
│  ├─ Filter: Id == request.GenerationId
│  ├─ Filter: UserId == userId (row-level security)
│  └─ Use AsTracking() for updates
│
├─ Check Generation exists
│  └─ Return NotFound if null
│
├─ Begin Transaction (implicit with SaveChanges)
│  │
│  ├─ Calculate acceptance counts
│  │  ├─ Count flashcards where source == 'ai-full'
│  │  └─ Count flashcards where source == 'ai-edited'
│  │
│  ├─ Create Flashcard entities
│  │  ├─ Set UserId = userId
│  │  ├─ Set GenerationId = request.GenerationId
│  │  ├─ Set CreatedAt = DateTime.UtcNow
│  │  ├─ Set UpdatedAt = DateTime.UtcNow
│  │  ├─ Trim Front and Back
│  │  └─ Add to context
│  │
│  ├─ Update Generation entity
│  │  ├─ Increment AcceptedUneditedCount by ai-full count
│  │  ├─ Increment AcceptedEditedCount by ai-edited count
│  │  └─ Set UpdatedAt = DateTime.UtcNow
│  │
│  └─ SaveChangesAsync (commits transaction)
│
├─ Map entities to FlashcardResponse DTOs
│
└─ Return Success with CreateFlashcardsBatchResponse
```

### Database Interactions:

- **Read:** Query Generation by Id and UserId (with tracking)
- **Write:** Insert multiple Flashcard records
- **Update:** Update Generation.AcceptedUneditedCount and AcceptedEditedCount
- **Transaction:** All operations wrapped in implicit EF Core transaction

## 6. Security Considerations

### Authentication

- Endpoint requires JWT Bearer token via `RequireAuthorization()`
- UserId extracted from `ClaimTypes.NameIdentifier` claim
- Invalid or missing token returns 401 Unauthorized

### Authorization (Row-Level Security)

- Generation ownership verified: `generation.UserId == userId`
- Prevents users from creating flashcards for other users' generations
- Flashcards automatically associated with authenticated user

### Input Validation

**Automatic (DataAnnotations):**

- GenerationId: Must be positive number (Range validation)
- Flashcards array: 1-50 items (MinLength/MaxLength)
- Front: 1-200 characters, required
- Back: 1-500 characters, required
- Source: Must match regex `^(ai-full|ai-edited)$`

**Manual (Service Layer):**

- UserId must not be empty GUID
- Generation must exist in database
- Generation must belong to authenticated user

### Data Protection

- SQL Injection: Protected by EF Core parameterized queries
- Mass Assignment: Prevented by using specific DTOs
- XSS: Input sanitization through trimming (Front/Back)
- No sensitive data in error messages

### Rate Limiting Considerations

- Maximum 50 flashcards per request limits potential abuse
- Consider implementing rate limiting middleware for production

## 7. Error Handling

### Error Scenarios and Responses:

**1. Authentication Failures (401 Unauthorized)**

- **Cause:** Missing JWT token
- **Cause:** Invalid JWT token
- **Cause:** Unable to extract userId from claims
- **Cause:** userId is not valid GUID
- **Handling:** Return `Results.Unauthorized()` at endpoint level
- **Logging:** Log warning with attempt details

**2. Validation Errors (400 Bad Request)**

- **Cause:** Empty flashcards array
- **Cause:** More than 50 flashcards
- **Cause:** Invalid front/back length
- **Cause:** Invalid source value (not 'ai-full' or 'ai-edited')
- **Cause:** GenerationId is not positive number
- **Handling:** Model binding returns validation problem details automatically
- **Response Format:** ProblemDetails with field-level errors
- **Logging:** Log warning with validation details

**3. Business Logic Errors (404 Not Found)**

- **Cause:** Generation with specified ID doesn't exist
- **Cause:** Generation exists but belongs to different user
- **Handling:** Return `Result.Failure("Generation not found or does not belong to user")`
- **Response:** 404 status with error message
- **Logging:** Log warning with userId and generationId

**4. Database Errors (500 Internal Server Error)**

- **Cause:** DbUpdateException during save
- **Cause:** Constraint violation
- **Cause:** Database connection failure
- **Handling:** Catch `DbUpdateException` specifically
- **Response:** Generic error message to client
- **Logging:** Log full exception with stack trace and context
- **Transaction:** Automatically rolled back by EF Core

**5. Unexpected Errors (500 Internal Server Error)**

- **Cause:** Any unhandled exception
- **Handling:** Catch general `Exception`
- **Response:** Generic error message
- **Logging:** Log full exception with stack trace
- **Transaction:** Automatically rolled back

### Error Handling Pattern:

```csharp
try {
    // Guard clauses (return early)
    // Business logic
    // Happy path
}
catch (DbUpdateException ex) {
    // Log and return specific database error
}
catch (Exception ex) {
    // Log and return generic error
}
```

### Logging Strategy:

- **Warning Level:** Invalid inputs, business rule violations
- **Error Level:** Database errors, unexpected exceptions
- **Information Level:** Successful operations
- **Include Context:** userId, generationId, flashcard count

## 8. Performance Considerations

### Database Optimization

1. **Batch Inserts:** EF Core automatically batches multiple inserts
2. **Single Transaction:** All operations in one database round-trip
3. **Tracking:** Use `AsTracking()` only for Generation (will be updated)
4. **Indexes:** Existing indexes on user_id and generation_id support queries
5. **Connection Pooling:** Leveraged by EF Core automatically

### Memory Management

- Maximum 50 flashcards limits memory footprint
- Use `List<Flashcard>` with known capacity for efficiency
- Proper disposal of DbContext through DI scoped lifetime

### Scalability Considerations

- **Concurrent Requests:** Each request uses separate DbContext (scoped)
- **Lock Contention:** Minimal - updates to different generation records don't conflict
- **Generation Updates:** Optimistic concurrency can be added if needed (UpdatedAt check)

### Potential Bottlenecks

1. **Database Write Speed:** Batch of 50 flashcards might take time

   - Mitigation: PostgreSQL handles bulk inserts efficiently

2. **Generation Lock:** Updating AcceptedUneditedCount/AcceptedEditedCount

   - Mitigation: Updates are fast; consider optimistic concurrency if conflicts occur

3. **Network Payload:** 50 flashcards can be large request/response

   - Mitigation: Already limited to 50; compression handled by middleware

### Caching Considerations

- Not applicable for write operations
- Consider caching generation metadata if read-heavy scenarios emerge

## 9. Implementation Steps

### Step 1: Update IFlashcardService Interface

**File:** `10xCards/Services/IFlashcardService.cs`

Add method signature:

```csharp
Task<Result<CreateFlashcardsBatchResponse>> CreateFlashcardsBatchAsync(
    Guid userId,
    CreateFlashcardsBatchRequest request,
    CancellationToken cancellationToken = default);
```

### Step 2: Implement Service Method in FlashcardService

**File:** `10xCards/Services/FlashcardService.cs`

Implement `CreateFlashcardsBatchAsync`:

- Add guard clauses for userId and request validation
- Query generation with `AsTracking()` and verify ownership
- Calculate acceptance counts from request.Flashcards
- Create flashcard entities in loop
- Update generation's AcceptedUneditedCount and AcceptedEditedCount
- Save changes in transaction
- Map entities to response DTOs
- Return Result.Success or Result.Failure with appropriate messages
- Add comprehensive error logging

### Step 3: Add Endpoint Mapping

**File:** `10xCards/Endpoints/FlashcardEndpoints.cs`

Add new endpoint in `MapFlashcardEndpoints`:

```csharp
group.MapPost("/batch", async (
    [FromBody] CreateFlashcardsBatchRequest request,
    IFlashcardService flashcardService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) => {
    
    // Extract userId from JWT claims
    // Call service method
    // Handle Result and return appropriate response
})
.WithName("CreateFlashcardsBatch")
.WithSummary("Create multiple flashcards from AI generation")
.WithDescription("Accepts 1-50 flashcards from generation and updates statistics")
.Produces<CreateFlashcardsBatchResponse>(StatusCodes.Status201Created)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);
```

### Step 4: Manual Testing

**File:** `10xCards/EndpointsTests/test-create-flashcard-endpoint.http` (or create new file)

Create HTTP test requests:

- Test successful batch creation (2-5 flashcards)
- Test with maximum 50 flashcards
- Test with invalid generationId
- Test with generation belonging to different user
- Test with validation errors (empty front, too long back, invalid source)
- Test with empty flashcards array
- Test with > 50 flashcards
- Test without authentication token

### Step 5: Verify Database State

After testing, verify:

- Flashcards created with correct data
- Generation.AcceptedUneditedCount updated correctly
- Generation.AcceptedEditedCount updated correctly
- Generation.UpdatedAt timestamp updated
- All flashcards have correct UserId
- All flashcards have correct GenerationId

### Step 6: Review and Refactor

- Ensure code follows clean code guidelines
- Verify error handling covers all scenarios
- Check logging is comprehensive
- Validate security measures are in place
- Ensure no linter errors

### Step 7: Documentation Update

If necessary, update:

- API documentation
- Service documentation comments
- README if endpoint list maintained

---

**Implementation Priority:** High (Core feature for AI generation flow)

**Estimated Complexity:** Medium (requires transaction handling and statistics calculation)

**Dependencies:**

- Existing models and DTOs (already created)
- FlashcardService infrastructure (already exists)
- Database entities and context (already configured)

### To-dos

- [ ] Add CreateFlashcardsBatchAsync method signature to IFlashcardService interface
- [ ] Implement CreateFlashcardsBatchAsync in FlashcardService with validation, transaction handling, and generation statistics update
- [ ] Add POST /api/flashcards/batch endpoint mapping in FlashcardEndpoints.cs
- [ ] Create HTTP test file with scenarios for success, validation errors, and edge cases
- [ ] Test and verify database state after batch creation including generation statistics