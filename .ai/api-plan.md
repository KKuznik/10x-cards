# REST API Plan for 10xCards

## 1. Resources

The API is organized around the following main resources, corresponding to database tables:

| Resource | Database Table | Description |
|----------|----------------|-------------|
| Auth | users | User authentication and account management |
| Flashcards | flashcards | Individual flashcard management (CRUD operations) |
| Generations | generations | AI generation tracking and statistics |

## 2. Endpoints

### 2.1 Authentication Endpoints

#### Register New User
**Endpoint:** `POST /api/auth/register`  
**Description:** Register a new user account (US-001)  
**Authentication:** None required  

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

**Success Response (201 Created):**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-04T12:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Validation failed
  ```json
  {
    "errors": {
      "email": ["Email is already registered"],
      "password": ["Password must be at least 8 characters"]
    }
  }
  ```

**Validation Rules:**
- Email must be valid format and unique
- Password minimum 8 characters, requires uppercase, lowercase, number, and special character
- Password and confirmPassword must match

---

#### Login
**Endpoint:** `POST /api/auth/login`  
**Description:** Authenticate user and receive JWT token (US-002)  
**Authentication:** None required  

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response (200 OK):**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-04T12:00:00Z"
}
```

**Error Responses:**
- `401 Unauthorized` - Invalid credentials
  ```json
  {
    "message": "Invalid email or password"
  }
  ```

---

#### Logout
**Endpoint:** `POST /api/auth/logout`  
**Description:** Invalidate current session  
**Authentication:** Required (Bearer token)  

**Request Body:** None

**Success Response (204 No Content)**

**Error Responses:**
- `401 Unauthorized` - Invalid or expired token

---

### 2.2 Flashcard Endpoints

#### List Flashcards
**Endpoint:** `GET /api/flashcards`  
**Description:** Retrieve paginated list of user's flashcards  
**Authentication:** Required (Bearer token)  

**Query Parameters:**
- `page` (integer, default: 1) - Page number
- `pageSize` (integer, default: 20, max: 100) - Items per page
- `source` (string, optional) - Filter by source: 'ai-full', 'ai-edited', 'manual'
- `sortBy` (string, default: 'createdAt') - Sort field: 'createdAt', 'updatedAt', 'front'
- `sortOrder` (string, default: 'desc') - Sort order: 'asc', 'desc'
- `search` (string, optional) - Search in front and back text

**Success Response (200 OK):**
```json
{
  "data": [
    {
      "id": 1,
      "front": "What is the capital of France?",
      "back": "Paris",
      "source": "manual",
      "createdAt": "2025-11-03T10:00:00Z",
      "updatedAt": "2025-11-03T10:00:00Z",
      "generationId": null
    },
    {
      "id": 2,
      "front": "What is photosynthesis?",
      "back": "The process by which plants convert light energy into chemical energy",
      "source": "ai-full",
      "createdAt": "2025-11-03T09:30:00Z",
      "updatedAt": "2025-11-03T09:30:00Z",
      "generationId": 5
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 3,
    "totalItems": 52
  }
}
```

**Error Responses:**
- `400 Bad Request` - Invalid query parameters
- `401 Unauthorized` - Missing or invalid token

---

#### Get Single Flashcard
**Endpoint:** `GET /api/flashcards/{id}`  
**Description:** Retrieve a specific flashcard by ID  
**Authentication:** Required (Bearer token)  

**Success Response (200 OK):**
```json
{
  "id": 1,
  "front": "What is the capital of France?",
  "back": "Paris",
  "source": "manual",
  "createdAt": "2025-11-03T10:00:00Z",
  "updatedAt": "2025-11-03T10:00:00Z",
  "generationId": null
}
```

**Error Responses:**
- `404 Not Found` - Flashcard not found or doesn't belong to user
  ```json
  {
    "message": "Flashcard not found"
  }
  ```
- `401 Unauthorized` - Missing or invalid token

---

#### Create Flashcard Manually
**Endpoint:** `POST /api/flashcards`  
**Description:** Create a single flashcard manually (US-007)  
**Authentication:** Required (Bearer token)  

**Request Body:**
```json
{
  "front": "What is the capital of France?",
  "back": "Paris"
}
```

**Success Response (201 Created):**
```json
{
  "id": 1,
  "front": "What is the capital of France?",
  "back": "Paris",
  "source": "manual",
  "createdAt": "2025-11-03T10:00:00Z",
  "updatedAt": "2025-11-03T10:00:00Z",
  "generationId": null
}
```

**Error Responses:**
- `400 Bad Request` - Validation failed
  ```json
  {
    "errors": {
      "front": ["Front is required", "Front must not exceed 200 characters"],
      "back": ["Back is required", "Back must not exceed 500 characters"]
    }
  }
  ```
- `401 Unauthorized` - Missing or invalid token

**Validation Rules:**
- `front`: Required, max 200 characters
- `back`: Required, max 500 characters

---

#### Create Multiple Flashcards from Generation
**Endpoint:** `POST /api/flashcards/batch`  
**Description:** Accept multiple flashcards from AI generation (US-004)  
**Authentication:** Required (Bearer token)  

**Request Body:**
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
      "front": "What pigment absorbs light in photosynthesis?",
      "back": "Chlorophyll, primarily found in chloroplasts",
      "source": "ai-edited"
    }
  ]
}
```

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
      "front": "What pigment absorbs light in photosynthesis?",
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
- `400 Bad Request` - Validation failed or invalid generationId
  ```json
  {
    "errors": {
      "flashcards[0].front": ["Front must not exceed 200 characters"],
      "flashcards[1].back": ["Back is required"]
    }
  }
  ```
- `404 Not Found` - Generation not found
- `401 Unauthorized` - Missing or invalid token

**Validation Rules:**
- `generationId`: Required, must exist and belong to user
- `flashcards`: Required array, min 1 item, max 50 items
- Each flashcard follows same validation as single create
- `source`: Must be 'ai-full' or 'ai-edited'

**Business Logic:**
- Updates generation record's `accepted_unedited_count` and `accepted_edited_count`
- Automatically calculates counts based on source field

---

#### Update Flashcard
**Endpoint:** `PUT /api/flashcards/{id}`  
**Description:** Update an existing flashcard (US-005)  
**Authentication:** Required (Bearer token)  

**Request Body:**
```json
{
  "front": "What is the capital of France?",
  "back": "Paris, located in the Île-de-France region"
}
```

**Success Response (200 OK):**
```json
{
  "id": 1,
  "front": "What is the capital of France?",
  "back": "Paris, located in the Île-de-France region",
  "source": "manual",
  "createdAt": "2025-11-03T10:00:00Z",
  "updatedAt": "2025-11-03T11:30:00Z",
  "generationId": null
}
```

**Error Responses:**
- `400 Bad Request` - Validation failed
- `404 Not Found` - Flashcard not found or doesn't belong to user
- `401 Unauthorized` - Missing or invalid token

**Validation Rules:**
- Same as create flashcard
- `updatedAt` automatically updated via database trigger

**Business Logic:**
- If flashcard was originally 'ai-full', update source to 'ai-edited'
- Preserve generationId if exists

---

#### Delete Flashcard
**Endpoint:** `DELETE /api/flashcards/{id}`  
**Description:** Delete a flashcard (US-006)  
**Authentication:** Required (Bearer token)  

**Success Response (204 No Content)**

**Error Responses:**
- `404 Not Found` - Flashcard not found or doesn't belong to user
- `401 Unauthorized` - Missing or invalid token

---

### 2.3 AI Generation Endpoints

#### Generate Flashcards
**Endpoint:** `POST /api/generations`  
**Description:** Generate flashcard suggestions using AI (US-003)  
**Authentication:** Required (Bearer token)  

**Request Body:**
```json
{
  "sourceText": "Photosynthesis is the process by which plants and other organisms convert light energy into chemical energy... [1000-10000 characters]",
  "model": "openai/gpt-4o-mini"
}
```

**Success Response (201 Created):**
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
      "back": "The process by which plants convert light energy into chemical energy"
    },
    {
      "front": "What pigment is essential for photosynthesis?",
      "back": "Chlorophyll, which gives plants their green color"
    }
  ]
}
```

**Error Responses:**
- `400 Bad Request` - Validation failed
  ```json
  {
    "errors": {
      "sourceText": ["Source text must be between 1000 and 10000 characters"],
      "model": ["Model is required"]
    }
  }
  ```
- `500 Internal Server Error` - AI generation failed
  ```json
  {
    "message": "Failed to generate flashcards",
    "errorCode": "AI_GENERATION_ERROR"
  }
  ```
- `503 Service Unavailable` - AI service temporarily unavailable
- `401 Unauthorized` - Missing or invalid token

**Validation Rules:**
- `sourceText`: Required, between 1000 and 10000 characters
- `model`: Required, valid OpenRouter model identifier

**Business Logic:**
- Calculate SHA-256 hash of sourceText for deduplication tracking
- Store generation metadata even if generation fails (via generation_error_logs table)
- Record generation_duration in milliseconds
- Return proposed flashcards directly (not saved to database yet)
- Error logging: If generation fails, create record in generation_error_logs with error_code and error_message

---

#### List Generations
**Endpoint:** `GET /api/generations`  
**Description:** Retrieve user's generation history with statistics  
**Authentication:** Required (Bearer token)  

**Query Parameters:**
- `page` (integer, default: 1)
- `pageSize` (integer, default: 20, max: 100)
- `sortBy` (string, default: 'createdAt')
- `sortOrder` (string, default: 'desc')

**Success Response (200 OK):**
```json
{
  "data": [
    {
      "id": 5,
      "model": "openai/gpt-4o-mini",
      "generatedCount": 8,
      "acceptedUneditedCount": 5,
      "acceptedEditedCount": 2,
      "sourceTextLength": 2450,
      "generationDuration": 3200,
      "createdAt": "2025-11-03T10:00:00Z",
      "acceptanceRate": 87.5
    },
    {
      "id": 4,
      "model": "anthropic/claude-3-haiku",
      "generatedCount": 10,
      "acceptedUneditedCount": 7,
      "acceptedEditedCount": 1,
      "sourceTextLength": 3200,
      "generationDuration": 4100,
      "createdAt": "2025-11-02T15:30:00Z",
      "acceptanceRate": 80.0
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 2,
    "totalItems": 12
  },
  "statistics": {
    "totalGenerations": 12,
    "totalGenerated": 95,
    "totalAccepted": 78,
    "overallAcceptanceRate": 82.1
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Missing or invalid token

---

#### Get Generation Details
**Endpoint:** `GET /api/generations/{id}`  
**Description:** Get detailed information about a specific generation  
**Authentication:** Required (Bearer token)  

**Success Response (200 OK):**
```json
{
  "id": 5,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "model": "openai/gpt-4o-mini",
  "generatedCount": 8,
  "acceptedUneditedCount": 5,
  "acceptedEditedCount": 2,
  "sourceTextHash": "a1b2c3d4e5f6...",
  "sourceTextLength": 2450,
  "generationDuration": 3200,
  "createdAt": "2025-11-03T10:00:00Z",
  "updatedAt": "2025-11-03T10:15:00Z",
  "flashcards": [
    {
      "id": 10,
      "front": "What is photosynthesis?",
      "back": "The process by which plants convert light energy into chemical energy",
      "source": "ai-full",
      "createdAt": "2025-11-03T10:15:00Z"
    },
    {
      "id": 11,
      "front": "What pigment absorbs light?",
      "back": "Chlorophyll",
      "source": "ai-edited",
      "createdAt": "2025-11-03T10:15:00Z"
    }
  ]
}
```

**Error Responses:**
- `404 Not Found` - Generation not found or doesn't belong to user
- `401 Unauthorized` - Missing or invalid token

---

## 3. Authentication and Authorization

### Authentication Mechanism

**Technology:** ASP.NET Core Identity with JWT (JSON Web Tokens)

**Implementation Details:**

1. **Token Generation:**
   - JWT tokens issued upon successful login/registration
   - Tokens include: userId, email, issued timestamp, expiration timestamp
   - Token expiration: 7 days (configurable)
   - Tokens signed with HS256 algorithm using secret key from configuration

2. **Token Usage:**
   - Include token in Authorization header: `Authorization: Bearer {token}`
   - All endpoints except `/api/auth/register` and `/api/auth/login` require valid token
   - Token validated on each request via ASP.NET Core authentication middleware

3. **Token Refresh:**
   - Out of scope for MVP
   - User must re-login after token expiration

4. **Security Considerations:**
   - Tokens stored client-side (localStorage or secure cookie)
   - HTTPS required for all API calls in production
   - Secret key stored in environment variables/Azure Key Vault

### Authorization Rules

**Row-Level Security (US-009):**

All data access enforced at application layer:

1. **Flashcards:**
   - Users can only access flashcards where `user_id` matches authenticated user
   - Enforced in EF Core queries: `.Where(f => f.UserId == currentUserId)`

2. **Generations:**
   - Users can only access generation records where `user_id` matches authenticated user
   - Same enforcement pattern as flashcards

**Implementation:**
- Base repository/service classes that automatically filter by userId
- Authorization attributes on controller actions: `[Authorize]`
- Custom authorization handlers for specific business rules if needed

---

## 4. Validation and Business Logic

### 4.1 Validation Rules by Resource

#### Flashcards

| Field | Rules |
|-------|-------|
| front | Required, max 200 characters, not empty/whitespace |
| back | Required, max 500 characters, not empty/whitespace |
| source | Required, must be one of: 'ai-full', 'ai-edited', 'manual' |
| user_id | Required, automatically set from authenticated user |
| generation_id | Optional, must exist if provided and belong to user |

#### Generations

| Field | Rules |
|-------|-------|
| sourceText | Required, between 1000 and 10000 characters |
| model | Required, valid OpenRouter model identifier |
| user_id | Required, automatically set from authenticated user |
| source_text_length | Calculated from sourceText |
| source_text_hash | Calculated SHA-256 hash of sourceText |
| generation_duration | Automatically calculated and stored in milliseconds |

#### Authentication

| Field | Rules |
|-------|-------|
| email | Required, valid email format, unique, max 255 characters |
| password | Required, min 8 characters, must contain uppercase, lowercase, number, special character |
| confirmPassword | Must match password (registration only) |


### 4.2 Business Logic Implementation

#### AI Generation Flow (US-003, US-004)

1. **Generation Request:**
   ```
   User submits text -> Validate length (1000-10000) -> Calculate hash -> 
   Call OpenRouter API -> Measure duration -> Parse response -> 
   Store generation record -> Return proposed flashcards
   ```

2. **Error Handling:**
   - If OpenRouter API fails: Store error in `generation_error_logs` table
   - Return user-friendly error message
   - Log error code, message, and metadata for debugging

3. **Acceptance Flow:**
   - User accepts/edits flashcards -> Submit via batch endpoint ->
   - Create flashcard records with generationId and source ->
   - Update generation record: calculate `accepted_unedited_count` and `accepted_edited_count` ->
   - Return created flashcards

#### Flashcard Management (US-005, US-006, US-007)

1. **Manual Creation:**
   - Set source to 'manual'
   - Set user_id from authenticated user
   - generationId remains null

2. **Editing:**
   - If original source was 'ai-full', update to 'ai-edited'
   - If original source was 'manual' or 'ai-edited', preserve source
   - Preserve generationId
   - Auto-update updatedAt via database trigger

3. **Deletion:**
   - Soft delete not implemented in MVP (hard delete)
   - Cascade handled at application level (no orphaned records)
   - Does not affect generation statistics (counts remain historical)

### 4.3 Error Handling Strategy

**Error Response Format:**
```json
{
  "message": "Human-readable error message",
  "errorCode": "MACHINE_READABLE_CODE",
  "errors": {
    "field": ["Specific validation error"]
  }
}
```

**HTTP Status Code Usage:**
- `200 OK` - Successful GET, PUT requests
- `201 Created` - Successful POST creating resources
- `204 No Content` - Successful DELETE or operations with no response body
- `400 Bad Request` - Validation errors, malformed requests
- `401 Unauthorized` - Missing, invalid, or expired authentication
- `403 Forbidden` - Authenticated but not authorized (rare in MVP due to simple auth model)
- `404 Not Found` - Resource doesn't exist or doesn't belong to user
- `500 Internal Server Error` - Unexpected server errors
- `503 Service Unavailable` - External service (OpenRouter) temporarily unavailable

**Logging:**
- All errors logged with correlation ID for tracing
- Sensitive data (passwords, tokens) never logged
- External API errors logged with full context for debugging

### 4.4 Performance Considerations

**Pagination:**
- Default page size: 20 items
- Maximum page size: 100 items
- Cursor-based pagination considered for future optimization

**Indexing:**
- Ensure indexes exist on: `user_id` (all tables), `generation_id` (flashcards)
- Additional indexes on `created_at`, `updated_at` for sorting

---

This REST API plan provides a comprehensive foundation for implementing the 10xCards application according to the PRD requirements and database schema while adhering to .NET 9, Blazor SSR, and Entity Framework Core best practices.

