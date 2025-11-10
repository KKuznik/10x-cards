<!-- 87c7b52a-b407-48f7-b97a-7758579ea9fd b77d71d0-7a54-44c7-9cf4-9b6ed1500cac -->
# Plan Implementacji: List Flashcards API Endpoint

## 1. Przegląd punktu końcowego

**Endpoint:** `GET /api/flashcards`

**Opis:** Pobiera stronicowaną listę fiszek należących do zalogowanego użytkownika. Endpoint umożliwia filtrowanie według źródła fiszek (AI wygenerowane bez edycji, AI wygenerowane z edycją, ręcznie utworzone), wyszukiwanie w tekście fiszek oraz sortowanie według różnych kryteriów.

**Uwierzytelnienie:** Wymagane (Bearer JWT token)

**User Story:** Użytkownik może przeglądać swoje fiszki z możliwością filtrowania, sortowania i wyszukiwania, aby łatwo znaleźć interesujące go materiały.

## 2. Szczegóły żądania

**Metoda HTTP:** GET

**Struktura URL:** `/api/flashcards?page={page}&pageSize={pageSize}&source={source}&sortBy={sortBy}&sortOrder={sortOrder}&search={search}`

**Nagłówki:**

- `Authorization: Bearer {jwt_token}` (wymagane)

**Query Parameters:**

| Parametr | Typ | Wymagany | Domyślna wartość | Walidacja | Opis |

|----------|-----|----------|------------------|-----------|------|

| `page` | integer | Nie | 1 | ≥ 1 | Numer strony |

| `pageSize` | integer | Nie | 20 | 1-100 | Liczba elementów na stronę |

| `source` | string | Nie | null (wszystkie) | 'ai-full', 'ai-edited', 'manual' | Filtr według źródła fiszki |

| `sortBy` | string | Nie | 'createdAt' | 'createdAt', 'updatedAt', 'front' | Pole sortowania |

| `sortOrder` | string | Nie | 'desc' | 'asc', 'desc' | Kierunek sortowania |

| `search` | string | Nie | null | max 200 znaków | Wyszukiwanie w `front` i `back` |

**Request Body:** Brak (GET request)

## 3. Wykorzystywane typy

### Istniejące typy (nie wymagają tworzenia):

**Request Model:**

- `ListFlashcardsQuery` - `10xCards/Models/Requests/ListFlashcardsQuery.cs`
  - Zawiera wszystkie query parameters z Data Annotations validation

**Response Models:**

- `FlashcardsListResponse` - `10xCards/Models/Responses/FlashcardsListResponse.cs`
  - Właściwości: `Data` (List<FlashcardResponse>), `Pagination` (PaginationMetadata)

- `FlashcardResponse` - `10xCards/Models/Responses/FlashcardResponse.cs`
  - Właściwości: `Id`, `Front`, `Back`, `Source`, `CreatedAt`, `UpdatedAt`, `GenerationId`

- `PaginationMetadata` - `10xCards/Models/Common/PaginationMetadata.cs`
  - Właściwości: `CurrentPage`, `PageSize`, `TotalPages`, `TotalItems`

**Entity:**

- `Flashcard` - `10xCards/Database/Entities/Flashcard.cs`
  - Entity Framework entity mapujący tabelę flashcards

## 4. Szczegóły odpowiedzi

### Success Response (200 OK):

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

**Content-Type:** `application/json`

### Error Responses:

**400 Bad Request** - Nieprawidłowe parametry query:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "PageSize": ["Page size must be between 1 and 100"],
    "Source": ["Source must be 'ai-full', 'ai-edited', or 'manual'"]
  }
}
```

**401 Unauthorized** - Brak lub nieprawidłowy token:

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**500 Internal Server Error** - Błąd serwera (obsługiwany przez GlobalExceptionHandlerMiddleware)

## 5. Przepływ danych

### Sekwencja działań:

1. **Request przychodzi do endpointu** → Middleware JWT weryfikuje token
2. **Ekstrakcja UserId z JWT claims** → ClaimTypes.NameIdentifier zawiera Guid użytkownika
3. **Walidacja query parameters** → ASP.NET Core automatycznie waliduje ListFlashcardsQuery
4. **Wywołanie FlashcardService.ListFlashcardsAsync()** → Przekazanie userId i query
5. **Service Query do bazy danych:**

   - Filtracja: `WHERE user_id = userId` (row-level security)
   - Filtracja opcjonalna: `AND source = query.Source` (jeśli source podane)
   - Wyszukiwanie: `AND (front ILIKE '%search%' OR back ILIKE '%search%')` (jeśli search podane)
   - Sortowanie: `ORDER BY {sortBy} {sortOrder}`
   - Stronicowanie: `.Skip((page - 1) * pageSize).Take(pageSize)`
   - Count query: `SELECT COUNT(*) FROM flashcards WHERE ...` (dla metadanych paginacji)

6. **Mapowanie Flashcard → FlashcardResponse** → Dla każdego elementu listy
7. **Utworzenie FlashcardsListResponse** → Z danymi i metadanymi paginacji
8. **Zwrócenie Result<FlashcardsListResponse>** → Do endpointu
9. **Endpoint zwraca 200 OK** → Z JSON response

### Interakcje z bazą danych:

**Główne zapytanie (z użyciem EF Core):**

```csharp
var query = _context.Flashcards
    .AsNoTracking() // Read-only query optimization
    .Where(f => f.UserId == userId);

// Optional source filter
if (!string.IsNullOrEmpty(request.Source))
    query = query.Where(f => f.Source == request.Source);

// Optional search filter
if (!string.IsNullOrEmpty(request.Search))
    query = query.Where(f => 
        EF.Functions.ILike(f.Front, $"%{request.Search}%") ||
        EF.Functions.ILike(f.Back, $"%{request.Search}%"));

// Total count for pagination
var totalItems = await query.CountAsync(cancellationToken);

// Sorting
query = request.SortBy switch {
    "front" => request.SortOrder == "asc" 
        ? query.OrderBy(f => f.Front) 
        : query.OrderByDescending(f => f.Front),
    "updatedAt" => request.SortOrder == "asc" 
        ? query.OrderBy(f => f.UpdatedAt) 
        : query.OrderByDescending(f => f.UpdatedAt),
    _ => request.SortOrder == "asc" 
        ? query.OrderBy(f => f.CreatedAt) 
        : query.OrderByDescending(f => f.CreatedAt)
};

// Pagination
var flashcards = await query
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);
```

**Indeksy wykorzystywane:**

- `IX_flashcards_user_id` - dla filtracji WHERE user_id
- Sortowanie może skorzystać z indeksów zależnie od sortBy

## 6. Względy bezpieczeństwa

### Uwierzytelnienie:

- **JWT Bearer Token** wymagany w nagłówku Authorization
- Middleware `app.UseAuthentication()` automatycznie weryfikuje token
- Token musi być ważny (nie wygasły, prawidłowy podpis, issuer, audience)

### Autoryzacja:

- Endpoint musi wymagać autoryzacji: `.RequireAuthorization()`
- **Row-Level Security (RLS)**: Użytkownik widzi tylko własne fiszki
  - Implementacja: `WHERE f.UserId == userId` w query
  - UserId pobierany z JWT claims (ClaimTypes.NameIdentifier)

### Walidacja danych wejściowych:

- **Automatyczna walidacja** przez ASP.NET Core z Data Annotations
- **Guard clauses** w service:
  - Sprawdzenie czy userId nie jest pustym Guid
  - Walidacja czy query nie jest null

### Ochrona przed atakami:

1. **SQL Injection**: 

   - Chronione przez EF Core (parametryzowane zapytania)
   - Wyszukiwanie przez `EF.Functions.ILike()` z parametrami

2. **Excessive Data Exposure**:

   - PageSize ograniczony do max 100 elementów
   - Paginacja zapobiega pobieraniu wszystkich rekordów

3. **Information Disclosure**:

   - W przypadku błędów zwracana jest generyczna wiadomość (przez GlobalExceptionHandlerMiddleware)
   - Stack trace tylko w Development mode

4. **Token Hijacking**:

   - HTTPS wymagany w production (app.UseHttpsRedirection())
   - Token powinien mieć rozsądny czas wygaśnięcia

## 7. Obsługa błędów

### Potencjalne błędy i ich obsługa:

| Scenariusz | Kod HTTP | Obsługa | Komunikat |

|------------|----------|---------|-----------|

| Nieprawidłowy page (< 1) | 400 | ASP.NET Validation | "Page must be at least 1" |

| Nieprawidłowy pageSize (< 1 lub > 100) | 400 | ASP.NET Validation | "Page size must be between 1 and 100" |

| Nieprawidłowy source | 400 | ASP.NET Validation | "Source must be 'ai-full', 'ai-edited', or 'manual'" |

| Nieprawidłowy sortBy | 400 | ASP.NET Validation | "SortBy must be 'createdAt', 'updatedAt', or 'front'" |

| Nieprawidłowy sortOrder | 400 | ASP.NET Validation | "SortOrder must be 'asc' or 'desc'" |

| Search > 200 znaków | 400 | ASP.NET Validation | "Search query must not exceed 200 characters" |

| Brak tokenu | 401 | JWT Middleware | Automatyczna odpowiedź 401 |

| Nieprawidłowy token | 401 | JWT Middleware | Automatyczna odpowiedź 401 |

| Token wygasł | 401 | JWT Middleware | Automatyczna odpowiedź 401 |

| Błąd bazy danych | 500 | GlobalExceptionHandlerMiddleware | "An error occurred while processing your request." |

| Brak połączenia z bazą | 500 | GlobalExceptionHandlerMiddleware | "An error occurred while processing your request." |

### Logowanie błędów:

**Service layer:**

```csharp
_logger.LogError(ex, 
    "Failed to list flashcards. UserId: {UserId}, Page: {Page}, PageSize: {PageSize}", 
    userId, query.Page, query.PageSize);
```

**Middleware:**

- GlobalExceptionHandlerMiddleware automatycznie loguje nieobsłużone wyjątki
- Dodaje correlation ID dla trackingu

### Edge cases:

1. **Pusta lista wyników**: Zwracane 200 OK z pustą tablicą `data: []` i `totalItems: 0`
2. **Page przekracza totalPages**: Zwracane 200 OK z pustą tablicą
3. **Search nie znajduje wyników**: Zwracane 200 OK z pustą tablicą
4. **Użytkownik nie ma żadnych fiszek**: Zwracane 200 OK z pustą tablicą

## 8. Rozważania dotyczące wydajności

### Potencjalne wąskie gardła:

1. **N+1 Query Problem**: 

   - **Rozwiązanie**: Używamy `.AsNoTracking()` dla read-only queries
   - Nie ładujemy navigation properties (User, Generation), więc brak N+1

2. **Brak indeksów**:

   - **Już zaimplementowane**: IX_flashcards_user_id
   - **Rozważyć**: Composite index na (user_id, created_at) dla domyślnego sortowania
   - **Rozważyć**: Full-text search index dla pola front i back (jeśli search będzie często używany)

3. **Duże zbiory danych**:

   - **Rozwiązanie**: Stronicowanie z limitem pageSize=100
   - Skip/Take w EF Core generuje OFFSET/LIMIT w SQL

4. **Wolne wyszukiwanie tekstowe**:

   - **Obecnie**: ILIKE z wildcards `%search%` (nie używa indeksów)
   - **Optymalizacja przyszła**: PostgreSQL Full-Text Search (tsvector, tsquery)
   - **Optymalizacja przyszła**: Elasticsearch dla zaawansowanego wyszukiwania

### Strategie optymalizacji:

1. **Database Query Optimization:**
   ```csharp
   .AsNoTracking() // Wyłączenie change tracking
   ```

2. **Response Caching** (opcjonalne, w przyszłości):

   - Dodać `[ResponseCache]` attribute lub OutputCache
   - Cache key: userId + query parameters hash
   - Invalidacja: przy CREATE/UPDATE/DELETE fiszki

3. **Compiled Queries** (dla bardzo częstych zapytań):
   ```csharp
   private static readonly Func<ApplicationDbContext, Guid, int, int, IAsyncEnumerable<Flashcard>> 
       CompiledQuery = EF.CompileAsyncQuery(...);
   ```

4. **Connection Pooling**:

   - Domyślnie włączone w EF Core
   - Konfiguracja w connection string: `Pooling=true;MinPoolSize=5;MaxPoolSize=100`

5. **Monitoring**:

   - Logowanie czasu wykonania query (jeśli > 1000ms)
   - Tracking średniego czasu odpowiedzi endpointu

## 9. Etapy wdrożenia

### Krok 1: Utworzenie interfejsu IFlashcardService

**Nowy plik:** `10xCards/Services/IFlashcardService.cs`

```csharp
using _10xCards.Models.Common;
using _10xCards.Models.Requests;
using _10xCards.Models.Responses;

namespace _10xCards.Services;

public interface IFlashcardService {
    Task<Result<FlashcardsListResponse>> ListFlashcardsAsync(
        Guid userId, 
        ListFlashcardsQuery query, 
        CancellationToken cancellationToken = default);
}
```

### Krok 2: Implementacja FlashcardService

**Nowy plik:** `10xCards/Services/FlashcardService.cs`

**Zależności:**

- `ApplicationDbContext` (scoped)
- `ILogger<FlashcardService>` (singleton)

**Logika:**

1. Guard clause: sprawdzenie czy userId nie jest pustym Guid
2. Query builder: bazowe query z filtrem user_id
3. Opcjonalne filtry: source, search (ILIKE)
4. Count query dla pagination metadata
5. Sortowanie: switch na sortBy i sortOrder
6. Stronicowanie: Skip/Take
7. Mapowanie: Flashcard → FlashcardResponse
8. Obliczenie TotalPages: (int)Math.Ceiling(totalItems / (double)pageSize)
9. Zwrócenie Result.Success z FlashcardsListResponse

**Obsługa błędów:**

- Try-catch dla database exceptions
- Logowanie błędów z userId i parametrami query
- Zwracanie Result.Failure z generycznym komunikatem

### Krok 3: Rejestracja serwisu w DI container

**Plik:** `10xCards/Program.cs`

Po linii z rejestracją AuthService:

```csharp
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
```

### Krok 4: Utworzenie FlashcardEndpoints

**Nowy plik:** `10xCards/Endpoints/FlashcardEndpoints.cs`

**Extension method:** `MapFlashcardEndpoints(this WebApplication app)`

**Endpoint GET /api/flashcards:**

```csharp
var group = app.MapGroup("/api/flashcards")
    .WithTags("Flashcards")
    .RequireAuthorization(); // Wymaga JWT

group.MapGet("", async (
    [AsParameters] ListFlashcardsQuery query,
    IFlashcardService flashcardService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) => 
{
    // Extract userId from JWT claims
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
        return Results.Unauthorized();
    }
    
    var result = await flashcardService.ListFlashcardsAsync(userId, query, cancellationToken);
    
    if (!result.IsSuccess) {
        return Results.BadRequest(new { message = result.ErrorMessage });
    }
    
    return Results.Ok(result.Value);
})
.WithName("ListFlashcards")
.WithSummary("Get paginated list of user's flashcards")
.WithDescription("Retrieves flashcards with optional filtering, sorting, and search")
.Produces<FlashcardsListResponse>(StatusCodes.Status200OK)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized);
```

**Uwaga:** `[AsParameters]` attribute umożliwia automatyczne bindowanie query parameters do obiektu ListFlashcardsQuery.

### Krok 5: Rejestracja endpointu w aplikacji

**Plik:** `10xCards/Program.cs`

Po linii `app.MapAuthEndpoints()`:

```csharp
app.MapFlashcardEndpoints();
```

### Krok 6: Dodanie using dla ClaimsPrincipal

**Plik:** `10xCards/Endpoints/FlashcardEndpoints.cs`

Upewnij się, że są dodane:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
```

### Krok 7: Testy manualne

**Test 1: Podstawowe pobranie listy**

```http
GET https://localhost:5001/api/flashcards
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 200 OK z listą fiszek

**Test 2: Paginacja**

```http
GET https://localhost:5001/api/flashcards?page=2&pageSize=10
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 200 OK z drugą stroną (10 elementów)

**Test 3: Filtrowanie według source**

```http
GET https://localhost:5001/api/flashcards?source=ai-full
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 200 OK z tylko fiszkami ai-full

**Test 4: Wyszukiwanie**

```http
GET https://localhost:5001/api/flashcards?search=France
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 200 OK z fiszkami zawierającymi "France"

**Test 5: Sortowanie**

```http
GET https://localhost:5001/api/flashcards?sortBy=front&sortOrder=asc
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 200 OK z fiszkami posortowanymi alfabetycznie po front

**Test 6: Brak autoryzacji**

```http
GET https://localhost:5001/api/flashcards
```

Oczekiwany: 401 Unauthorized

**Test 7: Nieprawidłowe parametry**

```http
GET https://localhost:5001/api/flashcards?pageSize=101
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 400 Bad Request z błędem walidacji

**Test 8: Pusta lista**

```http
GET https://localhost:5001/api/flashcards?page=999
Authorization: Bearer {valid_jwt_token}
```

Oczekiwany: 200 OK z pustą tablicą data

### Krok 8: Weryfikacja logów

Sprawdzić czy w logach aplikacji pojawiają się:

- Informacje o pomyślnych zapytaniach
- Błędy w przypadku problemów z bazą danych
- Correlation IDs w przypadku błędów 500

### Krok 9: Utworzenie pliku z testami HTTP

**Nowy plik:** `10xCards/EndpointsTests/test-list-flashcards-endpoint.http`

Utworzyć plik z testami manualnymi zawierający wszystkie scenariusze testowe:

- Test podstawowego pobrania listy (z tokenem)
- Test paginacji (różne wartości page i pageSize)
- Test filtrowania według source (ai-full, ai-edited, manual)
- Test wyszukiwania (search parameter)
- Test sortowania (różne kombinacje sortBy i sortOrder)
- Test bez autoryzacji (401)
- Test z nieprawidłowymi parametrami (400)
- Test pustej listy / kombinacji parametrów

**Uwaga:** Przed uruchomieniem testów trzeba:

1. Zarejestrować użytkownika lub użyć istniejącego
2. Skopiować JWT token z odpowiedzi rejestracji/logowania
3. Opcjonalnie: utworzyć kilka fiszek testowych dla użytkownika

### Krok 10: Weryfikacja buildu projektu

Sprawdzić czy projekt kompiluje się poprawnie:

```powershell
dotnet build
```

W przypadku błędów kompilacji, naprawić je przed kontynuacją.

### Krok 10: Code review checklist

- [ ] Service zarejestrowany w DI container
- [ ] Endpoint wymaga autoryzacji (.RequireAuthorization())
- [ ] UserId prawidłowo wyciągnięty z JWT claims
- [ ] Query używa AsNoTracking() dla wydajności
- [ ] Wszystkie edge cases obsłużone (pusta lista, page > totalPages)
- [ ] Błędy prawidłowo logowane z odpowiednimi parametrami
- [ ] Walidacja query parameters działa
- [ ] Response format zgodny ze specyfikacją API
- [ ] Pagination metadata prawidłowo obliczane
- [ ] Row-level security zaimplementowany (WHERE user_id = userId)

### To-dos

- [ ] Utworzenie interfejsu IFlashcardService w folderze Services
- [ ] Implementacja FlashcardService z logiką listowania fiszek
- [ ] Rejestracja FlashcardService w DI container (Program.cs)
- [ ] Utworzenie FlashcardEndpoints z GET /api/flashcards endpoint
- [ ] Rejestracja FlashcardEndpoints w Program.cs
- [ ] Wykonanie testów manualnych (8 scenariuszy)