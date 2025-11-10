<!-- d09fefd3-8f0a-478f-98ee-2bc2e030dd78 9a9a8055-95a5-4553-859d-501dfaadbfec -->
# API Endpoint Implementation Plan: Create Flashcard Manually

## 1. Przegląd punktu końcowego

Endpoint `POST /api/flashcards` umożliwia uwierzytelnionym użytkownikom ręczne tworzenie pojedynczej fiszki (flashcard). Fiszka składa się z frontu (pytania) i backu (odpowiedzi). Po pomyślnym utworzeniu endpoint zwraca status **201 Created** wraz z pełnymi danymi utworzonej fiszki, w tym automatycznie wygenerowanym ID, timestampami oraz metadanymi.

**Powiązane User Story:** US-007 (Create Flashcard Manually)

**Główne cele:**

- Umożliwienie użytkownikom tworzenia własnych fiszek bez użycia AI
- Walidacja danych wejściowych zgodnie z ograniczeniami bazy danych
- Automatyczne przypisanie fiszki do użytkownika z tokena JWT
- Oznaczenie źródła jako "manual" i brak powiązania z generacją AI

## 2. Szczegóły żądania

**Metoda HTTP:** `POST`

**Struktura URL:** `/api/flashcards`

**Nagłówki:**

- `Authorization: Bearer <jwt_token>` (wymagany)
- `Content-Type: application/json` (wymagany)

**Parametry:**

*Wymagane (w request body):*

- `front` (string): Pytanie na fiszce
  - Minimalnie: 1 znak
  - Maksymalnie: 200 znaków
  - Nie może być null lub empty

- `back` (string): Odpowiedź na fiszce
  - Minimalnie: 1 znak
  - Maksymalnie: 500 znaków
  - Nie może być null lub empty

*Opcjonalne:*

- Brak

*Automatycznie ustawiane przez system:*

- `userId`: Guid - ekstrahowany z JWT tokena (ClaimTypes.NameIdentifier)
- `source`: "manual" - oznacza ręczne utworzenie
- `generationId`: null - brak powiązania z generacją AI
- `createdAt`: DateTime UTC - timestamp utworzenia
- `updatedAt`: DateTime UTC - timestamp ostatniej aktualizacji (początkowo = createdAt)
- `id`: long - auto-generowany przez bazę danych (BIGSERIAL)

**Request Body (przykład):**

```json
{
  "front": "What is the capital of France?",
  "back": "Paris"
}
```

## 3. Wykorzystywane typy

**Request Model:**

- `CreateFlashcardRequest` (`10xCards/Models/Requests/CreateFlashcardRequest.cs`)
  - Już istnieje z odpowiednią walidacją (DataAnnotations)
  - Właściwości: `Front`, `Back`
  - Atrybuty walidacji: `[Required]`, `[MaxLength]`, `[MinLength]`

**Response Model:**

- `FlashcardResponse` (`10xCards/Models/Responses/FlashcardResponse.cs`)
  - Już istnieje
  - Właściwości: `Id`, `Front`, `Back`, `Source`, `CreatedAt`, `UpdatedAt`, `GenerationId`

**Entity:**

- `Flashcard` (`10xCards/Database/Entities/Flashcard.cs`)
  - Entity Framework entity mapujący tabelę `flashcards`
  - Właściwości: `Id`, `Front`, `Back`, `Source`, `CreatedAt`, `UpdatedAt`, `GenerationId`, `UserId`
  - Navigation properties: `User`, `Generation`

**Service Result Wrapper:**

- `Result<T>` (`10xCards/Models/Common/Result.cs`)
  - Generic wrapper dla odpowiedzi z serwisu
  - Właściwości: `IsSuccess`, `Value`, `ErrorMessage`
  - Metody: `Success(T value)`, `Failure(string errorMessage)`

## 4. Szczegóły odpowiedzi

### Success Response (201 Created)

**Status Code:** `201 Created`

**Content-Type:** `application/json`

**Body:**

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

**Pola odpowiedzi:**

- `id`: Unikalny identyfikator fiszki (auto-generowany)
- `front`: Pytanie na fiszce (dokładnie takie jak w request)
- `back`: Odpowiedź na fiszce (dokładnie taka jak w request)
- `source`: "manual" (zawsze dla tego endpointu)
- `createdAt`: Timestamp UTC utworzenia fiszki
- `updatedAt`: Timestamp UTC ostatniej aktualizacji (początkowo = createdAt)
- `generationId`: null (brak powiązania z generacją AI)

### Error Responses

**400 Bad Request - Nieprawidłowa walidacja:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "front": ["Front is required", "Front must not exceed 200 characters"],
    "back": ["Back is required", "Back must not exceed 500 characters"]
  }
}
```

*Scenariusze 400:*

- `front` jest pusty, null lub whitespace
- `back` jest pusty, null lub whitespace
- `front` przekracza 200 znaków
- `back` przekracza 500 znaków
- Request body jest nieprawidłowy JSON

**401 Unauthorized - Brak lub nieprawidłowy token:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

*Scenariusze 401:*

- Brak nagłówka Authorization
- Token JWT jest nieprawidłowy, wygasły lub uszkodzony
- Nie można wyekstrahować userId z tokena (brak ClaimTypes.NameIdentifier)
- userId nie jest poprawnym Guid

**500 Internal Server Error - Błąd serwera:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

*Scenariusze 500:*

- Błąd połączenia z bazą danych
- Timeout zapytania do bazy danych
- Nieoczekiwany wyjątek aplikacji
- Constraint violation (np. naruszenie CHECK constraint)

*Uwaga: Błędy 500 są obsługiwane przez GlobalExceptionHandlerMiddleware*

## 5. Przepływ danych

### Krok 1: Przyjęcie żądania HTTP

1. Użytkownik wysyła żądanie POST z JSON body zawierającym `front` i `back`
2. Middleware Authorization weryfikuje JWT token
3. ASP.NET Core Minimal API automatycznie deserializuje JSON do `CreateFlashcardRequest`
4. Model binding automatycznie waliduje DataAnnotations (Required, MaxLength, MinLength)
5. Jeśli walidacja fails → zwróć 400 Bad Request z szczegółami błędów

### Krok 2: Ekstrakcja userId z JWT

1. Endpoint ekstrahuje `ClaimsPrincipal` z HttpContext
2. Pobiera claim `ClaimTypes.NameIdentifier` (zawiera userId jako string)
3. Waliduje czy claim istnieje i czy można go sparsować do Guid
4. Jeśli nie → zwróć 401 Unauthorized

### Krok 3: Wywołanie serwisu

1. Endpoint wywołuje `IFlashcardService.CreateFlashcardAsync(userId, request, cancellationToken)`
2. Serwis przekazuje żądanie do warstwy biznesowej

### Krok 4: Logika biznesowa w serwisie (FlashcardService)

1. **Guard clause:** Sprawdzenie czy `userId == Guid.Empty` → jeśli tak, zwróć Result.Failure
2. **Guard clause:** Sprawdzenie czy `request == null` → jeśli tak, zwróć Result.Failure
3. **Utworzenie encji:**
   ```csharp
   var flashcard = new Flashcard {
       Front = request.Front.Trim(),
       Back = request.Back.Trim(),
       Source = "manual",
       GenerationId = null,
       UserId = userId,
       CreatedAt = DateTime.UtcNow,
       UpdatedAt = DateTime.UtcNow
   };
   ```

4. **Dodanie do DbContext:** `_context.Flashcards.Add(flashcard)`
5. **Zapisanie zmian:** `await _context.SaveChangesAsync(cancellationToken)`
6. **Mapowanie do DTO:**
   ```csharp
   var response = new FlashcardResponse {
       Id = flashcard.Id,
       Front = flashcard.Front,
       Back = flashcard.Back,
       Source = flashcard.Source,
       CreatedAt = flashcard.CreatedAt,
       UpdatedAt = flashcard.UpdatedAt,
       GenerationId = flashcard.GenerationId
   };
   ```

7. **Logowanie sukcesu:** `_logger.LogInformation("Successfully created flashcard...")`
8. **Zwrócenie wyniku:** `return Result<FlashcardResponse>.Success(response)`

### Krok 5: Obsługa wyjątków

- Try-catch w serwisie przechwytuje wszystkie wyjątki
- Logowanie błędu: `_logger.LogError(ex, "Failed to create flashcard...")`
- Zwrócenie Result.Failure z odpowiednim komunikatem

### Krok 6: Odpowiedź z endpointu

1. Endpoint sprawdza `result.IsSuccess`
2. Jeśli success → zwróć `Results.Created($"/api/flashcards/{result.Value.Id}", result.Value)`
3. Jeśli failure → zwróć `Results.BadRequest(new { message = result.ErrorMessage })`

### Diagram przepływu danych

```
[Client] 
   ↓ POST /api/flashcards + JWT
[Authorization Middleware] → weryfikacja tokena
   ↓
[Endpoint: POST /api/flashcards]
   ↓ Model Binding + Validation
   ↓ Ekstrakcja userId z JWT
   ↓
[IFlashcardService.CreateFlashcardAsync]
   ↓ Guard clauses
   ↓ Utworzenie Flashcard entity
   ↓
[ApplicationDbContext]
   ↓ SaveChangesAsync()
   ↓
[PostgreSQL Database: flashcards table]
   ↓ INSERT + auto-generated ID
   ↓
[FlashcardService]
   ↓ Mapowanie do FlashcardResponse
   ↓ Result.Success(response)
   ↓
[Endpoint]
   ↓ Results.Created(location, response)
   ↓
[Client] ← 201 Created + FlashcardResponse JSON
```

## 6. Względy bezpieczeństwa

### 6.1. Uwierzytelnianie (Authentication)

- **JWT Token:** Endpoint wymaga Bearer tokena w nagłówku Authorization
- **Middleware:** `.RequireAuthorization()` na grupie endpointów zapewnia weryfikację przez ASP.NET Core Identity
- **Token Validation:** Middleware automatycznie weryfikuje:
  - Sygnaturę tokena (HMAC SHA256)
  - Datę wygaśnięcia (exp claim)
  - Issuer i Audience (jeśli skonfigurowane)
  - Token nie został unieważniony

### 6.2. Autoryzacja (Authorization)

- **Row-Level Security:** Flashcard jest automatycznie przypisana do użytkownika z tokena (userId)
- **User Isolation:** Użytkownik może tworzyć fiszki tylko dla siebie (userId jest ekstrahowany z tokena, nie z request body)
- **No Privilege Escalation:** Niemożliwe jest utworzenie fiszki dla innego użytkownika

### 6.3. Walidacja danych wejściowych

- **DataAnnotations:** ASP.NET Core automatycznie waliduje:
  - Required fields
  - MaxLength constraints (200 dla front, 500 dla back)
  - MinLength constraints (1 dla obu)
- **Guard Clauses:** Dodatkowa walidacja w serwisie:
  - userId != Guid.Empty
  - request != null
- **Trimming:** `Front.Trim()` i `Back.Trim()` w serwisie, aby usunąć wiodące/końcowe whitespace
- **Database Constraints:** PostgreSQL weryfikuje:
  - NOT NULL constraints
  - VARCHAR length limits
  - CHECK constraint dla source (musi być 'manual', 'ai-full', 'ai-edited')

### 6.4. SQL Injection

- **Parametryzowane zapytania:** Entity Framework Core automatycznie parametryzuje wszystkie wartości
- **Brak raw SQL:** Używamy tylko EF Core LINQ queries
- **Zero risk:** Brak możliwości SQL injection

### 6.5. XSS (Cross-Site Scripting)

- **API-only endpoint:** Zwraca JSON, nie HTML
- **Frontend responsibility:** Walidacja i sanityzacja HTML jest odpowiedzialnością frontendu (Blazor)
- **No immediate risk:** API nie renderuje danych bezpośrednio do HTML

### 6.6. CSRF (Cross-Site Request Forgery)

- **JWT Authentication:** Tokeny w nagłówku Authorization, nie w cookies
- **SameSite not applicable:** CSRF protection nie jest konieczna dla JWT-based APIs

### 6.7. Rate Limiting

- **Not implemented:** Brak w aktualnej specyfikacji
- **Recommendation:** Warto rozważyć w przyszłości (np. 100 fiszek/godzinę na użytkownika)

### 6.8. CORS (Cross-Origin Resource Sharing)

- **Configuration required:** Jeśli API będzie używane z innej domeny
- **Middleware:** Należy skonfigurować CORS policy w Program.cs

### 6.9. Logging Security

- **No sensitive data:** Nie logować zawartości front/back (mogą zawierać dane wrażliwe)
- **Log userId:** Logować userId do audit trail
- **Structured logging:** Używać structured logging z ILogger

### 6.10. Error Information Disclosure

- **Generic error messages:** Nie ujawniać szczegółów implementacji w error messages
- **500 errors:** GlobalExceptionHandlerMiddleware mapuje wyjątki na generic 500 responses
- **Stack traces:** Nie zwracać stack traces do klienta w produkcji

## 7. Obsługa błędów

### 7.1. Kategorie błędów

#### Błędy klienta (4xx)

**400 Bad Request - Nieprawidłowa walidacja:**

- **Trigger:** DataAnnotations validation failure
- **Response:** ProblemDetails z listą błędów walidacji
- **Logging:** Warning level
- **Przykłady:**
  - `front` jest pusty: "Front is required"
  - `back` przekracza 500 znaków: "Back must not exceed 500 characters"
  - Nieprawidłowy JSON: "The JSON value could not be converted..."

**401 Unauthorized - Brak lub nieprawidłowy token:**

- **Trigger:** 
  - Brak nagłówka Authorization
  - Token wygasły lub nieprawidłowy
  - Nie można wyekstrahować userId
- **Response:** 401 Unauthorized (standardowy ASP.NET Core response)
- **Logging:** Warning level z userId (jeśli dostępne)
- **Przykład:** "Token validation failed" lub "Unauthorized"

#### Błędy serwera (5xx)

**500 Internal Server Error - Błąd aplikacji:**

- **Trigger:**
  - Błąd połączenia z bazą danych
  - Timeout zapytania
  - Constraint violation
  - Nieoczekiwany wyjątek
- **Response:** Generic ProblemDetails (bez szczegółów)
- **Logging:** Error level z pełnym stack trace
- **Handled by:** GlobalExceptionHandlerMiddleware

### 7.2. Strategia obsługi błędów w serwisie

```csharp
try {
    // Guard clauses (early returns)
    if (userId == Guid.Empty) {
        _logger.LogWarning("CreateFlashcardAsync called with empty userId");
        return Result<FlashcardResponse>.Failure("Invalid user ID");
    }
    
    if (request is null) {
        _logger.LogWarning("CreateFlashcardAsync called with null request. UserId: {UserId}", userId);
        return Result<FlashcardResponse>.Failure("Request is required");
    }
    
    // Business logic...
    
    // Happy path
    _logger.LogInformation(
        "Successfully created flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
        userId, flashcard.Id);
    
    return Result<FlashcardResponse>.Success(response);
}
catch (DbUpdateException ex) {
    _logger.LogError(ex, 
        "Database error while creating flashcard. UserId: {UserId}", userId);
    return Result<FlashcardResponse>.Failure(
        "An error occurred while saving the flashcard");
}
catch (Exception ex) {
    _logger.LogError(ex, 
        "Unexpected error while creating flashcard. UserId: {UserId}", userId);
    return Result<FlashcardResponse>.Failure(
        "An unexpected error occurred");
}
```

### 7.3. Logging Strategy

**Poziomy logowania:**

- **Information:** Pomyślne utworzenie fiszki (z userId i flashcardId)
- **Warning:** Guard clause failures, nieprawidłowy userId
- **Error:** Wyjątki bazy danych, nieoczekiwane wyjątki

**Structured logging:**

```csharp
_logger.LogInformation(
    "Successfully created flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}, Source: {Source}",
    userId, flashcard.Id, flashcard.Source);
```

**Co NIE logować:**

- Zawartość `front` i `back` (mogą zawierać dane wrażliwe)
- Pełny request body
- Token JWT

### 7.4. Tabela rejestracji błędów (generation_error_logs)

**NIE DOTYCZY** tego endpointu. Tabela `generation_error_logs` jest przeznaczona wyłącznie dla błędów generacji AI. Ręczne tworzenie fiszek nie wymaga rejestrowania w tej tabeli.

## 8. Rozważania dotyczące wydajności

### 8.1. Database Performance

**Wstawianie pojedynczej fiszki:**

- **Operacja:** Single INSERT do tabeli `flashcards`
- **Indeksy:** Automatyczne indeksowanie na `user_id` i `generation_id` (już istnieją)
- **Performance:** Bardzo szybka operacja (~1-5ms dla lokalnej DB)
- **Optymalizacja:** Brak potrzeby - pojedyncze wstawienie jest zawsze wydajne

**Connection Pooling:**

- **EF Core:** Automatycznie korzysta z connection pooling
- **Configuration:** Domyślne ustawienia są wystarczające dla tego endpointu

**Transaction:**

- **Implicit:** EF Core automatycznie owija SaveChangesAsync w transakcję
- **Rollback:** Automatyczny rollback w przypadku błędu
- **No explicit transaction needed:** Pojedyncza operacja nie wymaga jawnej transakcji

### 8.2. Memory Allocation

**Minimalna alokacja:**

- `CreateFlashcardRequest`: ~100 bytes (2 strings)
- `Flashcard` entity: ~200 bytes
- `FlashcardResponse`: ~150 bytes
- **Total:** ~450 bytes na request

**No memory leaks:**

- Wszystkie obiekty są scoped (garbage collected po request)
- Brak długo żyjących obiektów
- CancellationToken poprawnie propagowany

### 8.3. Concurrency

**Database-level concurrency:**

- **BIGSERIAL ID:** Automatycznie atomic i thread-safe
- **Multiple users:** Każdy użytkownik tworzy swoje fiszki (brak konfliktów)
- **No locking needed:** Brak konkurencyjnych zapisów do tego samego wiersza

**Application-level concurrency:**

- **Scoped services:** Każde żądanie ma własną instancję serwisu
- **DbContext:** Scoped lifetime, nie współdzielony między requestami
- **Thread-safe:** ASP.NET Core automatycznie zarządza wątkami

### 8.4. Caching

**Not applicable:**

- Endpoint tworzy nowe dane (POST)
- Brak cacheowalnych danych
- No cache headers needed

### 8.5. Potential Bottlenecks

**Database connection:**

- **Risk:** Bardzo niskie dla pojedynczych INSERT
- **Mitigation:** Connection pooling (już włączony)
- **Monitoring:** Śledzić czas odpowiedzi DB queries

**Network latency:**

- **Risk:** Niskie (małe payload ~200-700 bytes)
- **Mitigation:** Brak potrzeby kompresji dla tak małych danych

**Validation overhead:**

- **Risk:** Minimalne (DataAnnotations są bardzo szybkie)
- **Impact:** ~0.1-0.5ms

### 8.6. Scalability Considerations

**Horizontal scaling:**

- **Stateless endpoint:** Można łatwo skalować horyzontalnie
- **No session state:** Brak współdzielonego stanu między instancjami
- **Load balancer ready:** Endpoint działa za load balancerem bez problemów

**Database scaling:**

- **PostgreSQL:** Może obsłużyć tysiące INSERT/sec
- **Sharding:** Nie jest potrzebne (user_id jako naturalny klucz partycjonowania)
- **Read replicas:** Nie dotyczy (to jest write operation)

**Rate limiting recommendations:**

- **Per-user limit:** 100 fiszek/godzinę (zapobiega spamowi)
- **Global limit:** 10,000 requests/minutę (dla całej aplikacji)
- **Implementation:** Middleware lub API Gateway (np. NGINX)

### 8.7. Response Time Expectations

**Typical response times:**

- **Best case:** 5-10ms (lokalna sieć, mała DB)
- **Average case:** 20-50ms (normalne warunki produkcyjne)
- **Worst case:** 100-200ms (peak load, duża odległość od DB)

**SLA recommendations:**

- **p50:** < 50ms
- **p95:** < 100ms
- **p99:** < 200ms

## 9. Etapy wdrożenia

### Krok 1: Rozszerzenie interfejsu serwisu

**Plik:** `10xCards/Services/IFlashcardService.cs`

**Akcja:** Dodaj metodę do interfejsu:

```csharp
/// <summary>
/// Creates a new flashcard manually for a specific user
/// </summary>
/// <param name="userId">The ID of the user creating the flashcard</param>
/// <param name="request">The flashcard creation request</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result containing the created flashcard</returns>
Task<Result<FlashcardResponse>> CreateFlashcardAsync(
    Guid userId,
    CreateFlashcardRequest request,
    CancellationToken cancellationToken = default);
```

### Krok 2: Implementacja metody w serwisie

**Plik:** `10xCards/Services/FlashcardService.cs`

**Akcja:** Dodaj implementację metody `CreateFlashcardAsync`:

```csharp
public async Task<Result<FlashcardResponse>> CreateFlashcardAsync(
    Guid userId,
    CreateFlashcardRequest request,
    CancellationToken cancellationToken = default) {
    
    try {
        // Guard clause: validate userId
        if (userId == Guid.Empty) {
            _logger.LogWarning("CreateFlashcardAsync called with empty userId");
            return Result<FlashcardResponse>.Failure("Invalid user ID");
        }
        
        // Guard clause: validate request
        if (request is null) {
            _logger.LogWarning("CreateFlashcardAsync called with null request. UserId: {UserId}", userId);
            return Result<FlashcardResponse>.Failure("Request is required");
        }
        
        // Create new flashcard entity
        var flashcard = new Flashcard {
            Front = request.Front.Trim(),
            Back = request.Back.Trim(),
            Source = "manual",
            GenerationId = null,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Add to context and save
        _context.Flashcards.Add(flashcard);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Map to response DTO
        var response = new FlashcardResponse {
            Id = flashcard.Id,
            Front = flashcard.Front,
            Back = flashcard.Back,
            Source = flashcard.Source,
            CreatedAt = flashcard.CreatedAt,
            UpdatedAt = flashcard.UpdatedAt,
            GenerationId = flashcard.GenerationId
        };
        
        _logger.LogInformation(
            "Successfully created flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
            userId, flashcard.Id);
        
        // Happy path: return success
        return Result<FlashcardResponse>.Success(response);
    }
    catch (DbUpdateException ex) {
        _logger.LogError(ex, 
            "Database error while creating flashcard. UserId: {UserId}", userId);
        return Result<FlashcardResponse>.Failure(
            "An error occurred while saving the flashcard");
    }
    catch (Exception ex) {
        _logger.LogError(ex, 
            "Unexpected error while creating flashcard. UserId: {UserId}", userId);
        return Result<FlashcardResponse>.Failure(
            "An unexpected error occurred");
    }
}
```

**Wymagane using statements:**

```csharp
using _10xCards.Database.Entities;
using Microsoft.EntityFrameworkCore;
```

### Krok 3: Dodanie endpointu w FlashcardEndpoints

**Plik:** `10xCards/Endpoints/FlashcardEndpoints.cs`

**Akcja:** Dodaj mapowanie POST endpointu w metodzie `MapFlashcardEndpoints`, po istniejącym GET endpoincie:

```csharp
// POST /api/flashcards
group.MapPost("", async (
    [FromBody] CreateFlashcardRequest request,
    IFlashcardService flashcardService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) => {
    
    // Extract userId from JWT claims
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
        return Results.Unauthorized();
    }
    
    var result = await flashcardService.CreateFlashcardAsync(userId, request, cancellationToken);
    
    if (!result.IsSuccess) {
        return Results.BadRequest(new { message = result.ErrorMessage });
    }
    
    return Results.Created($"/api/flashcards/{result.Value.Id}", result.Value);
})
.WithName("CreateFlashcard")
.WithSummary("Create a new flashcard manually")
.WithDescription("Creates a single flashcard with front and back content")
.Produces<FlashcardResponse>(StatusCodes.Status201Created)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized);
```

**Lokalizacja:** Wewnątrz metody `MapFlashcardEndpoints`, po mapowaniu GET endpointu, przed `return app;`

### Krok 4: Weryfikacja konfiguracji DI

**Plik:** `10xCards/Program.cs`

**Akcja:** Sprawdź, czy `FlashcardService` jest już zarejestrowany w DI container. Jeśli nie, dodaj:

```csharp
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
```

**Uwaga:** Najprawdopodobniej jest już zarejestrowany, ponieważ endpoint GET działa.

### Krok 5: Weryfikacja konfiguracji Authorization

**Plik:** `10xCards/Program.cs`

**Akcja:** Upewnij się, że middleware Authorization jest poprawnie skonfigurowany:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

**Uwaga:** Najprawdopodobniej jest już skonfigurowany.

### Krok 6: Testowanie endpointu

**Narzędzie:** REST Client (np. Postman, .http files, curl)

**Test 1: Pomyślne utworzenie fiszki (201 Created)**

```http
POST https://localhost:5001/api/flashcards
Authorization: Bearer <valid_jwt_token>
Content-Type: application/json

{
  "front": "What is the capital of France?",
  "back": "Paris"
}
```

**Oczekiwany wynik:** 201 Created z FlashcardResponse

**Test 2: Brak tokenu (401 Unauthorized)**

```http
POST https://localhost:5001/api/flashcards
Content-Type: application/json

{
  "front": "Test",
  "back": "Test"
}
```

**Oczekiwany wynik:** 401 Unauthorized

**Test 3: Walidacja - pusty front (400 Bad Request)**

```http
POST https://localhost:5001/api/flashcards
Authorization: Bearer <valid_jwt_token>
Content-Type: application/json

{
  "front": "",
  "back": "Paris"
}
```

**Oczekiwany wynik:** 400 Bad Request z błędami walidacji

**Test 4: Walidacja - front zbyt długi (400 Bad Request)**

```http
POST https://localhost:5001/api/flashcards
Authorization: Bearer <valid_jwt_token>
Content-Type: application/json

{
  "front": "[string dłuższy niż 200 znaków]",
  "back": "Paris"
}
```

**Oczekiwany wynik:** 400 Bad Request z błędami walidacji

**Test 5: Weryfikacja w bazie danych**

```sql
SELECT * FROM flashcards WHERE user_id = '<user_id_from_token>' ORDER BY created_at DESC LIMIT 1;
```

**Oczekiwany wynik:** Nowa fiszka z `source = 'manual'`, `generation_id = NULL`

### Krok 7: Weryfikacja logów

**Akcja:** Sprawdź logi aplikacji pod kątem:

- Informacyjnych logów o pomyślnym utworzeniu fiszki
- Warning logów dla nieprawidłowych requestów
- Error logów dla wyjątków (jeśli wystąpiły)

**Przykładowe logi:**

```
[Information] Successfully created flashcard. UserId: 123e4567-e89b-12d3-a456-426614174000, FlashcardId: 1
[Warning] CreateFlashcardAsync called with empty userId
[Error] Database error while creating flashcard. UserId: 123e4567-e89b-12d3-a456-426614174000
```

### Krok 8: Testy integracyjne (opcjonalnie)

**Lokalizacja:** `10xCards.Tests/Integration/FlashcardEndpointsTests.cs` (jeśli projekt testowy istnieje)

**Scenariusze do przetestowania:**

- Pomyślne utworzenie fiszki z prawidłowymi danymi
- Błąd 401 dla nieprawidłowego tokena
- Błąd 400 dla nieprawidłowej walidacji
- Weryfikacja, że fiszka jest przypisana do właściwego użytkownika
- Weryfikacja, że source = "manual" i generationId = null

### Krok 9: Dokumentacja API (opcjonalnie)

**Plik:** `.ai/api-plan.md`

**Akcja:** Upewnij się, że dokumentacja endpointu jest aktualna (już powinna być)

**Swagger/OpenAPI:** Endpoint będzie automatycznie widoczny w Swagger UI dzięki:

- `.WithName("CreateFlashcard")`
- `.WithSummary(...)`
- `.WithDescription(...)`
- `.Produces<FlashcardResponse>(StatusCodes.Status201Created)`

### Krok 10: Finalizacja i merge

**Akcje końcowe:**

1. Przejrzyj wszystkie zmiany (code review)
2. Upewnij się, że wszystkie testy przechodzą
3. Sprawdź linter errors (jeśli są)
4. Zaktualizuj CHANGELOG.md (jeśli istnieje)
5. Utwórz Pull Request z odpowiednim opisem
6. Po zatwierdzeniu - merge do main branch

**Commit message example:**

```
feat(api): implement POST /api/flashcards endpoint

- Add CreateFlashcardAsync method to FlashcardService
- Add POST endpoint to FlashcardEndpoints
- Implement validation and error handling
- Add comprehensive logging
- Set source to "manual" and generationId to null

Closes #US-007
```

## 10. Podsumowanie

Endpoint `POST /api/flashcards` został zaprojektowany zgodnie z najlepszymi praktykami .NET 9, Entity Framework Core i ASP.NET Core Identity. Implementacja jest:

- **Bezpieczna:** JWT authentication, row-level security, walidacja danych
- **Wydajna:** Minimalna alokacja pamięci, szybkie INSERT do DB
- **Skalowalna:** Stateless, horizontal scaling ready
- **Testowalna:** Clean architecture, dependency injection
- **Maintainable:** Clean code, guard clauses, structured logging

Plan uwzględnia wszystkie aspekty: od walidacji po wydajność, bezpieczeństwo i obsługę błędów.

### To-dos

- [ ] Rozszerzyć IFlashcardService o metodę CreateFlashcardAsync
- [ ] Zaimplementować CreateFlashcardAsync w FlashcardService z guard clauses i error handling
- [ ] Dodać POST endpoint w FlashcardEndpoints.cs z właściwą walidacją i mapowaniem
- [ ] Sprawdzić konfigurację DI i authorization middleware w Program.cs
- [ ] Przetestować endpoint używając różnych scenariuszy (201, 400, 401)
- [ ] Zweryfikować, że fiszki są poprawnie zapisywane w bazie z source='manual'