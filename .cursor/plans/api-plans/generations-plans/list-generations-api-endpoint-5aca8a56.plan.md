<!-- 5aca8a56-e8fd-4d1f-8ea8-8146bc53920a 6b28c069-e2f1-43f2-b4a9-6eeef4569434 -->
# API Endpoint Implementation Plan: List Generations

## 1. Przegląd punktu końcowego

**Endpoint:** `GET /api/generations`

**Cel:** Pobranie paginowanej historii generacji AI użytkownika wraz ze statystykami akceptacji

Endpoint umożliwia użytkownikowi przeglądanie swoich wcześniejszych generacji fiszek przez AI, z możliwością sortowania i paginacji. Dla każdej generacji obliczana jest stopa akceptacji, a dodatkowo zwracane są ogólne statystyki wszystkich generacji użytkownika.

**Funkcjonalności:**

- Paginacja wyników (domyślnie 20 elementów na stronie, max 100)
- Sortowanie według daty utworzenia (rosnąco/malejąco)
- Automatyczne obliczanie współczynnika akceptacji dla każdej generacji
- Agregacja statystyk użytkownika (łączna liczba generacji, akceptacji, ogólny współczynnik)
- Filtrowanie danych tylko dla zalogowanego użytkownika (Row-Level Security)

## 2. Szczegóły żądania

**Metoda HTTP:** GET

**Struktura URL:** `/api/generations`

**Autoryzacja:** Wymagana (Bearer JWT token)

### Parametry zapytania (Query Parameters):

**Wszystkie parametry są opcjonalne z wartościami domyślnymi:**

- `page` (integer, default: 1)
                                                                                                                                - Minimum: 1
                                                                                                                                - Numer strony wyników do pobrania

- `pageSize` (integer, default: 20)
                                                                                                                                - Minimum: 1
                                                                                                                                - Maximum: 100
                                                                                                                                - Liczba elementów na stronie

- `sortBy` (string, default: "createdAt")
                                                                                                                                - Dozwolone wartości: tylko "createdAt"
                                                                                                                                - Kolumna według której sortować wyniki

- `sortOrder` (string, default: "desc")
                                                                                                                                - Dozwolone wartości: "asc", "desc"
                                                                                                                                - Kierunek sortowania (rosnąco/malejąco)

**Request Body:** Brak (GET request)

**Nagłówki:**

- `Authorization: Bearer {jwt_token}` (wymagany)
- `Accept: application/json`

## 3. Wykorzystywane typy

### Typy żądania:

- **`ListGenerationsQuery`** (`Models/Requests/ListGenerationsQuery.cs`)
                                                                                                                                - Już istnieje w projekcie
                                                                                                                                - Zawiera walidację Data Annotations dla wszystkich parametrów
                                                                                                                                - Properties: `Page`, `PageSize`, `SortBy`, `SortOrder`

### Typy odpowiedzi:

- **`GenerationsListResponse`** (`Models/Responses/GenerationsListResponse.cs`)
                                                                                                                                - Główna odpowiedź endpointu
                                                                                                                                - Properties: `Data`, `Pagination`, `Statistics`

- **`GenerationListItemResponse`** (`Models/Responses/GenerationListItemResponse.cs`)
                                                                                                                                - Pojedynczy element listy generacji
                                                                                                                                - Properties: `Id`, `Model`, `GeneratedCount`, `AcceptedUneditedCount`, `AcceptedEditedCount`, `SourceTextLength`, `GenerationDuration`, `CreatedAt`, `AcceptanceRate`
                                                                                                                                - `AcceptanceRate` - obliczany w serwisie

- **`PaginationMetadata`** (`Models/Common/PaginationMetadata.cs`)
                                                                                                                                - Metadata paginacji
                                                                                                                                - Properties: `CurrentPage`, `PageSize`, `TotalPages`, `TotalItems`

- **`GenerationStatistics`** (`Models/Responses/GenerationStatistics.cs`)
                                                                                                                                - Statystyki użytkownika
                                                                                                                                - Properties: `TotalGenerations`, `TotalGenerated`, `TotalAccepted`, `OverallAcceptanceRate`

- **`Result<T>`** (`Models/Common/Result.cs`)
                                                                                                                                - Wrapper dla wyników operacji serwisu
                                                                                                                                - Używany wewnętrznie między serwisem a endpointem

### Encje bazodanowe:

- **`Generation`** (`Database/Entities/Generation.cs`)
                                                                                                                                - Entity Framework model tabeli `generations`
                                                                                                                                - Mapowanie na `generation_list_item_response` z obliczeniem `AcceptanceRate`

## 4. Szczegóły odpowiedzi

### Sukces (200 OK):

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

### Błąd 401 Unauthorized:

Zwracany gdy:

- Brak tokena JWT w nagłówku Authorization
- Token jest nieprawidłowy lub wygasły
- Brak lub nieprawidłowy claim UserId w tokenie

Odpowiedź: Standardowa odpowiedź 401 z ASP.NET Core

### Błąd 400 Bad Request:

Zwracany gdy parametry nie przechodzą walidacji:

```json
{
  "message": "Validation error message"
}
```

Przykłady błędów walidacji:

- `Page` < 1: "Page must be at least 1"
- `PageSize` poza zakresem 1-100: "Page size must be between 1 and 100"
- `SortBy` nie jest 'createdAt': "SortBy must be 'createdAt'"
- `SortOrder` nie jest 'asc' lub 'desc': "SortOrder must be 'asc' or 'desc'"

### Błąd 500 Internal Server Error:

Zwracany przy nieoczekiwanych błędach serwera:

- Błąd połączenia z bazą danych
- Nieobsłużony wyjątek w logice biznesowej

## 5. Przepływ danych

### Sekwencja operacji:

1. **Endpoint otrzymuje żądanie** (`FlashcardEndpoints.cs` lub nowy `GenerationEndpoints.cs`)

                                                                                                                                                                                                - Model binding parametrów query do `ListGenerationsQuery`
                                                                                                                                                                                                - Automatyczna walidacja Data Annotations

2. **Autoryzacja i ekstrakcja UserId**

                                                                                                                                                                                                - Middleware ASP.NET Core weryfikuje JWT token
                                                                                                                                                                                                - Endpoint ekstraktuje `UserId` z `ClaimsPrincipal` (claim: `NameIdentifier`)
                                                                                                                                                                                                - Walidacja formatu GUID

3. **Wywołanie serwisu** (`IGenerationService.ListGenerationsAsync`)

                                                                                                                                                                                                - Przekazanie `UserId` i `ListGenerationsQuery`
                                                                                                                                                                                                - Zwracany `Result<GenerationsListResponse>`

4. **Operacje w serwisie:**

a. **Zapytanie do bazy danych** (przez `ApplicationDbContext`):

   ```
   - Filtrowanie: WHERE user_id = {userId}
   - Sortowanie: ORDER BY created_at {asc|desc}
   - Paginacja: SKIP (page-1)*pageSize TAKE pageSize
   - AsNoTracking() dla read-only query
   ```

b. **Obliczenie AcceptanceRate** dla każdego rekordu:

   ```
   AcceptanceRate = ((AcceptedUneditedCount ?? 0) + (AcceptedEditedCount ?? 0)) / GeneratedCount * 100
   ```

Obsługa nullable wartości - traktować jako 0

c. **Pobranie statystyk użytkownika** (osobne zapytanie agregujące):

   ```
   - TotalGenerations = COUNT(*)
   - TotalGenerated = SUM(generated_count)
   - TotalAccepted = SUM(accepted_unedited_count + accepted_edited_count)
   - OverallAcceptanceRate = (TotalAccepted / TotalGenerated) * 100
   ```

d. **Obliczenie metadanych paginacji:**

   ```
   - CurrentPage = query.Page
   - PageSize = query.PageSize
   - TotalItems = COUNT zapytania bez SKIP/TAKE
   - TotalPages = CEILING(TotalItems / PageSize)
   ```

e. **Mapowanie** encji `Generation` na `GenerationListItemResponse`

f. **Budowa odpowiedzi** `GenerationsListResponse`

5. **Zwrot wyniku do endpointu**

                                                                                                                                                                                                - Jeśli `Result.IsSuccess` = true: `Results.Ok(result.Value)` → 200
                                                                                                                                                                                                - Jeśli `Result.IsSuccess` = false: `Results.BadRequest(...)` → 400

### Interakcje z bazą danych:

**Tabela:** `generations`

**Wykorzystywane kolumny:**

- `id`, `user_id`, `model`, `generated_count`, `accepted_unedited_count`, `accepted_edited_count`, `source_text_length`, `generation_duration`, `created_at`

**Indeksy używane:**

- Index na `user_id` (dla filtrowania)
- Możliwy composite index na `(user_id, created_at)` dla optymalizacji sortowania

**Zapytania:**

1. Zapytanie główne: Paginowana lista z sortowaniem
2. Zapytanie statystyk: Agregacja dla całego zbioru użytkownika

## 6. Względy bezpieczeństwa

### Autoryzacja i Autentykacja:

1. **JWT Bearer Authentication**

                                                                                                                                                                                                - Endpoint wymaga atrybutu `RequireAuthorization()`
                                                                                                                                                                                                - Token weryfikowany przez middleware ASP.NET Core Identity
                                                                                                                                                                                                - Sprawdzenie obecności i ważności tokena

2. **Row-Level Security (RLS)**

                                                                                                                                                                                                - Wszystkie zapytania filtrowane po `UserId` z JWT
                                                                                                                                                                                                - Użytkownik widzi tylko swoje dane
                                                                                                                                                                                                - Brak możliwości dostępu do generacji innych użytkowników

3. **Walidacja UserId**

                                                                                                                                                                                                - Sprawdzenie obecności claim `NameIdentifier`
                                                                                                                                                                                                - Walidacja formatu GUID
                                                                                                                                                                                                - Guard clause - early return przy błędzie

### Walidacja danych wejściowych:

1. **Parametry Query**

                                                                                                                                                                                                - Data Annotations w `ListGenerationsQuery`
                                                                                                                                                                                                - Automatyczna walidacja przez model binding
                                                                                                                                                                                                - Validation errors → 400 Bad Request

2. **SQL Injection Prevention**

                                                                                                                                                                                                - Entity Framework Core używa parametryzowanych zapytań
                                                                                                                                                                                                - Brak bezpośredniego SQL
                                                                                                                                                                                                - LINQ zapewnia bezpieczeństwo

3. **Input Sanitization**

                                                                                                                                                                                                - `SortBy` - whitelist tylko "createdAt"
                                                                                                                                                                                                - `SortOrder` - whitelist tylko "asc" lub "desc"
                                                                                                                                                                                                - Nie pozwala na arbitrary column sorting

### Information Disclosure:

- Nie zwracać `SourceTextHash` (dane wewnętrzne)
- Nie ujawniać stacktrace w production
- Structured logging bez wrażliwych danych
- Generic error messages dla 500 errors

### Rate Limiting (opcjonalne):

- Rozważyć implementację rate limiting dla GET endpoints
- Zapobieganie DoS przez nadmierne zapytania
- ASP.NET Core middleware lub zewnętrzne rozwiązania (Redis)

## 7. Obsługa błędów

### Kody statusu i scenariusze:

#### 200 OK

**Kiedy:** Pomyślne pobranie danych (nawet jeśli lista pusta)

**Przypadki:**

- Użytkownik ma generacje - zwrócona paginowana lista
- Użytkownik nie ma generacji - zwrócona pusta lista `data: []` z `totalItems: 0`

#### 400 Bad Request

**Kiedy:** Błędy walidacji parametrów wejściowych

**Przykłady:**

- `page` = 0 lub ujemne
- `pageSize` > 100 lub < 1
- `sortBy` nie jest "createdAt"
- `sortOrder` nie jest "asc" ani "desc"

**Odpowiedź:**

```json
{
  "message": "Validation error message"
}
```

**Logowanie:** INFO level - normalne błędy użytkownika

#### 401 Unauthorized

**Kiedy:** Problemy z autoryzacją

**Przykłady:**

- Brak nagłówka Authorization
- Token JWT wygasły
- Token JWT nieprawidłowy/zmanipulowany
- Brak claim UserId w tokenie
- UserId nie jest prawidłowym GUID

**Odpowiedź:** Standardowa 401 z ASP.NET Core

**Logowanie:** WARNING level

#### 500 Internal Server Error

**Kiedy:** Nieoczekiwane błędy serwera

**Przykłady:**

- Błąd połączenia z bazą danych
- Timeout bazy danych
- Wyjątek w obliczeniach (dzielenie przez zero)
- OutOfMemory exception
- Nieobsłużony wyjątek w kodzie

**Obsługa:**

- Global exception handling middleware
- Mapowanie na ProblemDetails (RFC 7807)
- Generic error message (nie ujawniać szczegółów)

**Odpowiedź:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

**Logowanie:** ERROR level z pełnym stacktrace i correlation ID

### Strategie obsługi błędów:

1. **Guard Clauses**

                                                                                                                                                                                                - Sprawdzenie UserId na początku endpointu
                                                                                                                                                                                                - Early return dla błędów autoryzacji

2. **Try-Catch w serwisie**

                                                                                                                                                                                                - Catch database exceptions
                                                                                                                                                                                                - Return `Result.Failure()` z odpowiednim message
                                                                                                                                                                                                - Log exception details

3. **Structured Logging**

                                                                                                                                                                                                - `ILogger` dependency injection
                                                                                                                                                                                                - Correlation IDs dla śledzenia requestów
                                                                                                                                                                                                - Log levels: INFO (walidacja), WARNING (auth), ERROR (server)

4. **Division by Zero Protection**

                                                                                                                                                                                                - Sprawdzenie `TotalGenerated > 0` przed obliczeniem `OverallAcceptanceRate`
                                                                                                                                                                                                - Jeśli 0, ustawić `OverallAcceptanceRate = 0.0`

## 8. Rozważania dotyczące wydajności

### Potencjalne wąskie gardła:

1. **Zapytania do bazy danych**

                                                                                                                                                                                                - Dwa zapytania: lista + statystyki
                                                                                                                                                                                                - Dla użytkowników z dużą liczbą generacji może być wolne

2. **Obliczenia AcceptanceRate**

                                                                                                                                                                                                - Obliczane dla każdego rekordu
                                                                                                                                                                                                - W C# (in-memory) po pobraniu z bazy

3. **Large result sets**

                                                                                                                                                                                                - Użytkownicy z setkami generacji
                                                                                                                                                                                                - PageSize max 100 ogranicza

### Strategie optymalizacji:

#### 1. Database Optimization

**Indeksy:**

- Upewnić się, że istnieje index na `user_id` (już jest)
- Rozważyć composite index: `(user_id, created_at DESC)` dla optymalizacji sortowania
- Monitoring execution plans

**Query optimization:**

- `AsNoTracking()` - **obowiązkowe** (read-only query)
- `AsNoTrackingWithIdentityResolution()` - jeśli potrzebna identity resolution
- Projection - SELECT tylko potrzebne kolumny (nie cała encja)

**Connection pooling:**

- EF Core automatycznie używa connection pooling
- Sprawdzić konfigurację connection string

#### 2. Caching

**Response Caching:**

- `[ResponseCache]` attribute lub middleware
- Cache na 30-60 sekund (dane rzadko się zmieniają)
- `Cache-Control` headers
- Invalidacja cache po utworzeniu nowej generacji

**Distributed Caching (opcjonalne):**

- Redis dla większej skali
- Cache statistics (rzadko się zmieniają)
- Cache per user + TTL

#### 3. Code Optimization

**Parallel computation:**

- Nie dotyczy - single user query

**Compiled Queries:**

- Dla często wykonywanych zapytań
- EF Core 8+ compiled models

**Projection in query:**

```csharp
var query = context.Generations
    .Where(g => g.UserId == userId)
    .Select(g => new GenerationListItemResponse {
        Id = g.Id,
        Model = g.Model,
        // ... map fields
    });
```

#### 4. Monitoring

- Application Insights lub Serilog
- Tracking query execution time
- Slow query alerts (> 500ms)
- Database performance metrics

### Performance Targets:

- **P50 latency:** < 100ms
- **P95 latency:** < 300ms
- **P99 latency:** < 500ms
- **Database query time:** < 50ms

### Skalowanie:

- Stateless service - łatwo skalować horizontal
- Database read replicas dla read-heavy workload
- Load balancing

## 9. Etapy wdrożenia

### Faza 1: Przygotowanie struktury

**1.1. Utworzenie interfejsu serwisu**

- Plik: `10xCards/Services/IGenerationService.cs`
- Metoda: `Task<Result<GenerationsListResponse>> ListGenerationsAsync(Guid userId, ListGenerationsQuery query, CancellationToken cancellationToken = default)`
- XML documentation comments

**1.2. Implementacja serwisu**

- Plik: `10xCards/Services/GenerationService.cs`
- Dependency: `ApplicationDbContext`, `ILogger<GenerationService>`
- Constructor injection

### Faza 2: Logika biznesowa serwisu

**2.1. Implementacja `ListGenerationsAsync`**

a. **Walidacja wejściowa** (guard clauses)

- Sprawdzenie czy userId nie jest pustym GUID

b. **Zapytanie główne - lista generacji**

```csharp
var query = _context.Generations
    .AsNoTracking()
    .Where(g => g.UserId == userId);

// Sortowanie
query = query.SortOrder == "asc" 
    ? query.OrderBy(g => g.CreatedAt)
    : query.OrderByDescending(g => g.CreatedAt);

// Paginacja
var items = await query
    .Skip((query.Page - 1) * query.PageSize)
    .Take(query.PageSize)
    .ToListAsync(cancellationToken);
```

c. **Mapowanie na DTOs z obliczeniem AcceptanceRate**

```csharp
var data = items.Select(g => new GenerationListItemResponse {
    // ... mapping
    AcceptanceRate = g.GeneratedCount > 0 
        ? ((g.AcceptedUneditedCount ?? 0) + (g.AcceptedEditedCount ?? 0)) / (double)g.GeneratedCount * 100 
        : 0.0
}).ToList();
```

d. **Zapytanie statystyk**

```csharp
var stats = await _context.Generations
    .AsNoTracking()
    .Where(g => g.UserId == userId)
    .GroupBy(g => g.UserId)
    .Select(group => new GenerationStatistics {
        TotalGenerations = group.Count(),
        TotalGenerated = group.Sum(g => g.GeneratedCount),
        TotalAccepted = group.Sum(g => (g.AcceptedUneditedCount ?? 0) + (g.AcceptedEditedCount ?? 0))
    })
    .FirstOrDefaultAsync(cancellationToken);

// Obliczenie OverallAcceptanceRate
if (stats != null && stats.TotalGenerated > 0) {
    stats.OverallAcceptanceRate = stats.TotalAccepted / (double)stats.TotalGenerated * 100;
}
```

e. **Metadata paginacji**

```csharp
var totalItems = await _context.Generations
    .AsNoTracking()
    .Where(g => g.UserId == userId)
    .CountAsync(cancellationToken);

var pagination = new PaginationMetadata {
    CurrentPage = query.Page,
    PageSize = query.PageSize,
    TotalItems = totalItems,
    TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize)
};
```

f. **Budowa odpowiedzi**

```csharp
var response = new GenerationsListResponse {
    Data = data,
    Pagination = pagination,
    Statistics = stats ?? new GenerationStatistics()
};

return Result<GenerationsListResponse>.Success(response);
```

**2.2. Obsługa błędów**

- Try-catch dla database exceptions
- Logging z ILogger
- Return Result.Failure() z odpowiednim message

### Faza 3: Endpoint API

**3.1. Utworzenie lub rozszerzenie pliku endpoints**

Opcja A: Rozszerzyć istniejący `FlashcardEndpoints.cs` (jeśli generacje są blisko związane)

Opcja B: Utworzyć nowy `GenerationEndpoints.cs` (zalecane dla separacji)

**3.2. Implementacja endpointu GET /api/generations**

```csharp
public static class GenerationEndpoints {
    public static WebApplication MapGenerationEndpoints(this WebApplication app) {
        var group = app.MapGroup("/api/generations")
            .WithTags("Generations")
            .RequireAuthorization();

        group.MapGet("", async (
            [AsParameters] ListGenerationsQuery query,
            IGenerationService generationService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) => {

            // Extract userId from JWT claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
                return Results.Unauthorized();
            }

            var result = await generationService.ListGenerationsAsync(userId, query, cancellationToken);

            if (!result.IsSuccess) {
                return Results.BadRequest(new { message = result.ErrorMessage });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ListGenerations")
        .WithSummary("Get paginated list of user's generations")
        .WithDescription("Retrieves generation history with statistics and acceptance rates")
        .Produces<GenerationsListResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
```

### Faza 4: Rejestracja zależności

**4.1. Dependency Injection w Program.cs**

```csharp
builder.Services.AddScoped<IGenerationService, GenerationService>();
```

**4.2. Mapowanie endpointu w Program.cs**

```csharp
app.MapGenerationEndpoints(); // lub w FlashcardEndpoints jeśli tam zaimplementowano
```

### Faza 5: Testowanie

**5.1. Unit testy serwisu**

- Mock `ApplicationDbContext`
- Test obliczenia AcceptanceRate
- Test obliczenia OverallAcceptanceRate
- Test edge cases (pusta lista, division by zero)
- Test paginacji

**5.2. Integration testy endpointu**

- Test autoryzacji (brak tokena, nieprawidłowy token)
- Test walidacji parametrów
- Test paginacji (różne page/pageSize)
- Test sortowania (asc/desc)
- Test Response 200 z danymi
- Test Response 200 z pustą listą
- Test Response 401 Unauthorized

**5.3. Testy manualne**

- Plik: `10xCards/EndpointsTests/GenerationTests/test-list-generations-endpoint.http`
- Test cases:
                                                                                                                                - Valid request z domyślnymi parametrami
                                                                                                                                - Valid request z custom paginacją
                                                                                                                                - Valid request z sortowaniem asc
                                                                                                                                - Invalid page parameter
                                                                                                                                - Invalid pageSize parameter
                                                                                                                                - Invalid sortBy parameter
                                                                                                                                - Invalid sortOrder parameter
                                                                                                                                - Unauthorized request (brak tokena)

### Faza 6: Dokumentacja i finalizacja

**6.1. Swagger/OpenAPI**

- Automatycznie generowany z atrybutów `.Produces()`
- Sprawdzić poprawność w Swagger UI

**6.2. Code review checklist**

- AsNoTracking() używany
- Guard clauses na początku metod
- Proper error handling i logging
- XML documentation comments
- Structured logging z correlation IDs
- Walidacja wszystkich parametrów
- Security - RLS przez UserId filter

**6.3. Performance testing**

- Load testing z dużą liczbą rekordów
- Sprawdzić execution plans zapytań SQL
- Monitoring query performance

**6.4. Deployment**

- Merge do main branch
- CI/CD pipeline (GitHub Actions)
- Deploy na DigitalOcean
- Smoke tests na production

---

## Podsumowanie kluczowych punktów implementacji:

✅ **Typy DTO** - już istnieją, nie trzeba tworzyć

✅ **Nowy serwis** - `IGenerationService` + `GenerationService`

✅ **Endpoint** - Nowy plik `GenerationEndpoints.cs` lub rozszerzenie `FlashcardEndpoints.cs`

✅ **Obliczenia** - AcceptanceRate i statistics w serwisie

✅ **Bezpieczeństwo** - JWT + RLS przez UserId filter

✅ **Wydajność** - AsNoTracking(), indexy, caching opcjonalnie

✅ **Error handling** - Guard clauses, try-catch, Result pattern

Priorytetyzacja: Najpierw core functionality (Faza 1-4), potem testy (Faza 5), na końcu optymalizacje.

### To-dos

- [ ] Utworzyć interfejs IGenerationService z metodą ListGenerationsAsync
- [ ] Zaimplementować GenerationService z logiką biznesową, obliczeniami i zapytaniami do bazy
- [ ] Utworzyć endpoint GET /api/generations z autoryzacją i walidacją
- [ ] Zarejestrować serwis w DI i zmapować endpoint w Program.cs
- [ ] Utworzyć testy manualne w pliku .http dla endpointu
- [ ] Zweryfikować wydajność zapytań i rozważyć optymalizacje