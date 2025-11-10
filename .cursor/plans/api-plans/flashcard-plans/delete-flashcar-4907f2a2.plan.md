<!-- 4907f2a2-20a7-4e60-b486-67e66470e3b9 2bca35bb-0614-4a3b-839b-bc13eaf13200 -->
# API Endpoint Implementation Plan: Delete Flashcard

## 1. Przegląd punktu końcowego

Endpoint DELETE /api/flashcards/{id} umożliwia uwierzytelnionym użytkownikom usunięcie własnych fiszek. Implementacja musi zapewnić Row-Level Security, aby użytkownicy mogli usuwać tylko swoje własne fiszki. Endpoint nie zwraca żadnych danych w przypadku sukcesu (204 No Content).

## 2. Szczegóły żądania

- **Metoda HTTP:** DELETE
- **Struktura URL:** `/api/flashcards/{id}`
- **Parametry:**
  - **Wymagane:**
    - `id` (long) - ID fiszki w route parameter
    - Bearer token w nagłówku Authorization (automatycznie walidowany przez middleware)
  - **Opcjonalne:** Brak
- **Request Body:** Brak

## 3. Wykorzystywane typy

- `Result<bool>` - zwracany z warstwy serwisu (lub można użyć Result<Unit> jeśli preferowana jest semantyka "no value")
- `Flashcard` - entity z Database.Entities
- `ClaimsPrincipal` - do ekstrakcji userId z JWT token
- `ILogger<FlashcardService>` - do logowania

Nie są potrzebne nowe DTOs ani Request/Response modele - endpoint zwraca 204 No Content bez body.

## 4. Szczegóły odpowiedzi

**Sukces (204 No Content):**

- Brak body w odpowiedzi
- Headers: standardowe

**Błędy:**

- **401 Unauthorized** - brak lub nieprawidłowy JWT token
  ```json
  (brak body, automatyczny response z middleware)
  ```

- **404 Not Found** - fiszka nie istnieje lub nie należy do użytkownika
  ```json
  {
    "message": "Flashcard not found or does not belong to user"
  }
  ```

- **500 Internal Server Error** - błąd bazy danych lub nieoczekiwany wyjątek
  ```json
  {
    "message": "An error occurred while deleting the flashcard"
  }
  ```


## 5. Przepływ danych

1. **Request przychodzi do endpointu** (`DELETE /api/flashcards/{id}`)

   - Middleware autoryzacji waliduje JWT token (zwraca 401 jeśli invalid)
   - Endpoint ekstraktuje `userId` z `ClaimsPrincipal`
   - Endpoint ekstraktuje `id` z route parameter

2. **Wywołanie warstwy serwisu:**
   ```
   var result = await flashcardService.DeleteFlashcardAsync(userId, id, cancellationToken);
   ```

3. **Warstwa serwisu (`FlashcardService.DeleteFlashcardAsync`):**

   - Waliduje userId (guard clause)
   - Wykonuje query do bazy danych z filtrem userId i flashcardId (RLS):
     ```csharp
     var flashcard = await _context.Flashcards
         .Where(f => f.Id == flashcardId && f.UserId == userId)
         .FirstOrDefaultAsync(cancellationToken);
     ```

   - Jeśli null → zwraca `Result<bool>.Failure("Flashcard not found or does not belong to user")`
   - Jeśli znaleziono → usuwa entity:
     ```csharp
     _context.Flashcards.Remove(flashcard);
     await _context.SaveChangesAsync(cancellationToken);
     ```

   - Loguje operację (info przy sukcesie, error przy błędach)
   - Zwraca `Result<bool>.Success(true)`

4. **Endpoint mapuje Result na HTTP response:**

   - Sukces → `Results.NoContent()` (204)
   - Failure z "not found" → `Results.NotFound(new { message })` (404)
   - Inne błędy → `Results.BadRequest(new { message })` (400) lub `Results.Problem()` (500)

5. **Brak interakcji z zewnętrznymi serwisami** - tylko baza danych przez EF Core

## 6. Względy bezpieczeństwa

### Uwierzytelnianie

- Endpoint wymaga autoryzacji poprzez `.RequireAuthorization()` w MapGroup
- JWT Bearer token walidowany przez ASP.NET Core Identity middleware
- Token musi zawierać claim `ClaimTypes.NameIdentifier` z userId

### Autoryzacja (Row-Level Security)

- Query do bazy danych zawiera **podwójny filtr:**
  ```csharp
  .Where(f => f.Id == flashcardId && f.UserId == userId)
  ```

- To zapewnia, że użytkownik może usunąć tylko swoje fiszki
- Nawet jeśli użytkownik zna ID cudzej fiszki, nie może jej usunąć

### Walidacja danych wejściowych

- `id` automatycznie walidowany jako `long` przez routing ASP.NET Core
- `userId` walidowany w serwisie (guard clause: `userId == Guid.Empty`)
- Brak możliwości SQL injection (EF Core używa zapytań parametryzowanych)

### Informacje o błędach

- Nie ujawniamy czy fiszka istnieje czy nie należy do użytkownika (ta sama wiadomość dla obu przypadków)
- Szczegółowe błędy logowane po stronie serwera, ale nie zwracane do klienta

## 7. Obsługa błędów

### W warstwie serwisu (`FlashcardService.DeleteFlashcardAsync`):

**Guard clauses (wczesne zwroty):**

```csharp
if (userId == Guid.Empty) {
    _logger.LogWarning("DeleteFlashcardAsync called with empty userId");
    return Result<bool>.Failure("Invalid user ID");
}
```

**Fiszka nie znaleziona:**

```csharp
if (flashcard is null) {
    _logger.LogWarning(
        "DeleteFlashcardAsync: Flashcard not found or does not belong to user. UserId: {UserId}, FlashcardId: {FlashcardId}",
        userId, flashcardId);
    return Result<bool>.Failure("Flashcard not found or does not belong to user");
}
```

**Błędy bazy danych:**

```csharp
catch (DbUpdateException ex) {
    _logger.LogError(ex,
        "Database error while deleting flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
        userId, flashcardId);
    return Result<bool>.Failure("An error occurred while deleting the flashcard");
}
```

**Nieoczekiwane wyjątki:**

```csharp
catch (Exception ex) {
    _logger.LogError(ex,
        "Unexpected error while deleting flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
        userId, flashcardId);
    return Result<bool>.Failure("An unexpected error occurred");
}
```

### W warstwie endpointu (`FlashcardEndpoints.MapDelete`):

**Brak lub nieprawidłowy userId:**

```csharp
if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
    return Results.Unauthorized();
}
```

**Mapowanie Result na HTTP response:**

```csharp
if (!result.IsSuccess) {
    if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true) {
        return Results.NotFound(new { message = result.ErrorMessage });
    }
    return Results.BadRequest(new { message = result.ErrorMessage });
}
return Results.NoContent();
```

## 8. Rozważania dotyczące wydajności

### Optymalizacje:

1. **Tracking query:** Używamy standardowego query (NIE AsNoTracking), ponieważ potrzebujemy śledzenia entity do usunięcia
2. **Single query:** Jedna operacja SELECT + DELETE (przez `FirstOrDefaultAsync` + `Remove` + `SaveChangesAsync`)
3. **Indeksy:** Database posiada indeks na `user_id` i primary key na `id` - zapewnia szybkie wyszukiwanie
4. **Cancellation token:** Przekazywany do wszystkich async operacji dla graceful cancellation

### Potencjalne wąskie gardła:

- Brak szczególnych wąskich gardeł - operacja DELETE jest prosta i szybka
- Connection pooling EF Core automatycznie zarządza połączeniami do bazy danych
- Brak potrzeby cachingu (operacja modyfikująca dane)

### Transakcje:

- EF Core automatycznie opakowuje `SaveChangesAsync` w transakcję
- Dla pojedynczego DELETE nie ma potrzeby ręcznej kontroli transakcji

## 9. Etapy wdrożenia

### Krok 1: Rozszerzenie interfejsu IFlashcardService

Plik: `10xCards/Services/IFlashcardService.cs`

Dodaj deklarację metody na końcu interfejsu (przed zamykającym nawiasem):

```csharp
/// <summary>
/// Deletes a flashcard for a specific user
/// </summary>
/// <param name="userId">The ID of the user deleting the flashcard</param>
/// <param name="flashcardId">The ID of the flashcard to delete</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result indicating success or failure</returns>
Task<Result<bool>> DeleteFlashcardAsync(
    Guid userId,
    long flashcardId,
    CancellationToken cancellationToken = default);
```

### Krok 2: Implementacja metody w FlashcardService

Plik: `10xCards/Services/FlashcardService.cs`

Dodaj metodę na końcu klasy FlashcardService (przed zamykającym nawiasem):

```csharp
public async Task<Result<bool>> DeleteFlashcardAsync(
    Guid userId,
    long flashcardId,
    CancellationToken cancellationToken = default) {

    try {
        // Guard clause: validate userId
        if (userId == Guid.Empty) {
            _logger.LogWarning("DeleteFlashcardAsync called with empty userId");
            return Result<bool>.Failure("Invalid user ID");
        }

        // Query flashcard with tracking (will be deleted)
        var flashcard = await _context.Flashcards
            .Where(f => f.Id == flashcardId && f.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        // Guard clause: verify flashcard exists and belongs to user
        if (flashcard is null) {
            _logger.LogWarning(
                "DeleteFlashcardAsync: Flashcard not found or does not belong to user. UserId: {UserId}, FlashcardId: {FlashcardId}",
                userId, flashcardId);
            return Result<bool>.Failure("Flashcard not found or does not belong to user");
        }

        // Remove flashcard from context
        _context.Flashcards.Remove(flashcard);
        
        // Save changes to database
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully deleted flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
            userId, flashcardId);

        // Happy path: return success
        return Result<bool>.Success(true);
    }
    catch (DbUpdateException ex) {
        _logger.LogError(ex,
            "Database error while deleting flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
            userId, flashcardId);
        return Result<bool>.Failure(
            "An error occurred while deleting the flashcard");
    }
    catch (Exception ex) {
        _logger.LogError(ex,
            "Unexpected error while deleting flashcard. UserId: {UserId}, FlashcardId: {FlashcardId}",
            userId, flashcardId);
        return Result<bool>.Failure(
            "An unexpected error occurred");
    }
}
```

### Krok 3: Dodanie endpointu w FlashcardEndpoints

Plik: `10xCards/Endpoints/FlashcardEndpoints.cs`

Dodaj endpoint DELETE przed `return app;` w metodzie `MapFlashcardEndpoints`:

```csharp
// DELETE /api/flashcards/{id}
group.MapDelete("/{id:long}", async (
    long id,
    IFlashcardService flashcardService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) => {

    // Extract userId from JWT claims
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
        return Results.Unauthorized();
    }

    var result = await flashcardService.DeleteFlashcardAsync(userId, id, cancellationToken);

    if (!result.IsSuccess) {
        // Check for "not found" error for 404 response
        if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true) {
            return Results.NotFound(new { message = result.ErrorMessage });
        }
        return Results.BadRequest(new { message = result.ErrorMessage });
    }

    return Results.NoContent();
})
.WithName("DeleteFlashcard")
.WithSummary("Delete a flashcard")
.WithDescription("Deletes a flashcard owned by the authenticated user")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem(StatusCodes.Status400BadRequest);
```

### Krok 4: Testowanie

Utwórz plik testowy: `10xCards/EndpointsTests/test-delete-flashcard-endpoint.http`

```http
### Test 1: Delete existing flashcard (Success - 204)
DELETE {{baseUrl}}/api/flashcards/1
Authorization: Bearer {{authToken}}

### Test 2: Delete non-existent flashcard (404)
DELETE {{baseUrl}}/api/flashcards/999999
Authorization: Bearer {{authToken}}

### Test 3: Delete without authentication (401)
DELETE {{baseUrl}}/api/flashcards/1

### Test 4: Delete flashcard belonging to another user (404)
DELETE {{baseUrl}}/api/flashcards/{{otherUserFlashcardId}}
Authorization: Bearer {{authToken}}
```

### Krok 5: Weryfikacja i dokumentacja

- Przetestuj wszystkie scenariusze z pliku `.http`
- Zweryfikuj logi w konsoli dla każdego przypadku
- Sprawdź czy fiszka została faktycznie usunięta z bazy danych
- Zaktualizuj dokumentację API jeśli istnieje

---

**Uwagi końcowe:**

- Implementacja nie wymaga migracji bazy danych (brak zmian w schemacie)
- Nie ma potrzeby dodawania nowych typów DTO
- Relacja z `generations` jest ON DELETE SET NULL, więc usunięcie fiszki nie wpływa na tabelę generations
- Implementacja jest zgodna z wzorcem stosowanym w pozostałych endpointach

### To-dos

- [ ] Dodać metodę DeleteFlashcardAsync do interfejsu IFlashcardService
- [ ] Zaimplementować metodę DeleteFlashcardAsync w klasie FlashcardService
- [ ] Dodać endpoint DELETE /api/flashcards/{id} w FlashcardEndpoints
- [ ] Utworzyć plik testowy z przypadkami testowymi dla endpointu DELETE
- [ ] Przetestować wszystkie scenariusze i zweryfikować poprawność działania